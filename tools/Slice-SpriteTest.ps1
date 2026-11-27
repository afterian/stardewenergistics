param(
  [Parameter(Mandatory)][string]$InputPng,
  [Parameter(Mandatory)][string]$OutputDirectory,
  [ValidateRange(1,20)][int]$Columns=4,
  [ValidateRange(0.15,0.85)][double]$RowSplit=0.59,
  [ValidateRange(1,255)][int]$AlphaCutoff=200
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$inputPath=(Resolve-Path -LiteralPath $InputPng).Path
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw 'Use a new output folder to preserve existing work.'}
$bm=[Drawing.Bitmap]::new($inputPath)
try{
  $w=$bm.Width; $h=$bm.Height
  $rowEdge=@(0,[int][Math]::Round($h*$RowSplit),$h)
  if($bm.GetPixel(0,0).A -ge $AlphaCutoff -or $bm.GetPixel(($w-1),($h-1)).A -ge $AlphaCutoff){throw 'Opaque corners: use a transparent source sheet.'}
  $cells=@()
  for($row=0;$row -lt 2;$row++){
    for($col=0;$col -lt $Columns;$col++){
      $x0=[int][Math]::Floor($w*$col/$Columns);$x1=[int][Math]::Floor($w*($col+1)/$Columns)
      $y0=$rowEdge[$row];$y1=$rowEdge[$row+1]
      $l=$x1;$t=$y1;$rr=-1;$bb=-1
      for($y=$y0;$y -lt $y1;$y++){for($x=$x0;$x -lt $x1;$x++){
        if($bm.GetPixel($x,$y).A -ge $AlphaCutoff){
          if($x -lt $l){$l=$x};if($x -gt $rr){$rr=$x};if($y -lt $t){$t=$y};if($y -gt $bb){$bb=$y}
        }
      }}
      if($rr -lt 0){throw "Empty cell $row,$col"}
      if($l -eq $x0 -or $rr -eq ($x1-1) -or $t -eq $y0 -or $bb -eq ($y1-1)){throw "Sprite touches cell edge at $row,$col; adjust grid."}
      $cells += [pscustomobject]@{row=$row;col=$col;l=$l;t=$t;r=$rr;b=$bb;w=$rr-$l+1;h=$bb-$t+1; cx=($l+$rr+1)/2; base=$bb+1}
    }
  }
  # Preserve relative alignment for the first two machine states.
  if($Columns -ge 2){
    $a=$cells[0];$b=$cells[1];$dx=[Math]::Floor($w/$Columns)
    $b.cx=$a.cx+$dx;$b.base=$a.base
  }
  [void][IO.Directory]::CreateDirectory($out)
  $preview=[Drawing.Bitmap]::new(($Columns*176),480,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $gfx=[Drawing.Graphics]::FromImage($preview)
  $gfx.Clear([Drawing.Color]::FromArgb(65,48,38))
  $gfx.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
  $gfx.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
  $font=[Drawing.Font]::new('Segoe UI',10,[Drawing.FontStyle]::Bold)
  $tiles=@(); $metrics=@()
  try{
    for($row=0;$row -lt 2;$row++){
      $nativeH=if($row -eq 0){32}else{16}
      $cc=@($cells|Where-Object row -eq $row)
      $maxW=($cc|Measure-Object -Property w -Maximum).Maximum
      $maxH=($cc|Measure-Object -Property h -Maximum).Maximum
      $scale=[Math]::Min(14/$maxW,($nativeH-2)/$maxH)
      $atlas=[Drawing.Bitmap]::new(($Columns*16),$nativeH,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
      try{
        foreach($c in $cc){
          $kind=if($row -eq 0){'machine'}else{'resource'}
          $id='{0}-{1:d2}' -f $kind,($c.col+1)
          $tile=[Drawing.Bitmap]::new(16,$nativeH,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
          $colors=[Collections.Generic.HashSet[int]]::new();$visible=0
          try{
            for($iy=0;$iy -lt $nativeH;$iy++){for($ix=0;$ix -lt 16;$ix++){
              $sx=[int][Math]::Floor($c.cx+($ix+0.5-8)/$scale)
              $sy=[int][Math]::Floor($c.base+($iy+0.5-($nativeH-1))/$scale)
              if($sx -lt $c.l -or $sx -gt $c.r -or $sy -lt $c.t -or $sy -gt $c.b){continue}
              $p=$bm.GetPixel($sx,$sy)
              if($p.A -ge $AlphaCutoff){
                $v=[Drawing.Color]::FromArgb(255,$p.R,$p.G,$p.B)
                $tile.SetPixel($ix,$iy,$v);$atlas.SetPixel(($c.col*16+$ix),$iy,$v)
                $visible++;[void]$colors.Add($v.ToArgb())
              }
            }}
            if(!$visible){throw "Empty native output $id"}
            $tile.Save((Join-Path $out "$id.png"),[Drawing.Imaging.ImageFormat]::Png)
            $xx=$c.col*176+24;$yy=if($row -eq 0){25}else{330}
            $gfx.DrawImage($tile,[Drawing.Rectangle]::new($xx,$yy,128,($nativeH*8)))
            $gfx.DrawString("$id  •  8x",$font,[Drawing.Brushes]::Wheat,$xx,($yy+$nativeH*8))
            $metrics += [pscustomobject]@{id=$id;sourceBounds=@($c.l,$c.t,$c.w,$c.h);size=@(16,$nativeH);opaquePixels=$visible;rgbColors=$colors.Count}
          }finally{$tile.Dispose()}
        }
        $atlas.Save((Join-Path $out "$kind-sheet.png"),[Drawing.Imaging.ImageFormat]::Png)
      }finally{$atlas.Dispose()}
    }
    $preview.Save((Join-Path $out 'batch-test-01-native-preview.png'),[Drawing.Imaging.ImageFormat]::Png)
    $metrics | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'metrics.json') -Encoding utf8
    $metrics|Format-Table -AutoSize
  }finally{$font.Dispose();$gfx.Dispose();$preview.Dispose()}
}finally{$bm.Dispose()}

