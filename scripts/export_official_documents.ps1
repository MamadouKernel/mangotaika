$ErrorActionPreference = 'Stop'

$root = 'C:\Users\kerne\Downloads\rodi\new\MangoTaika'
$sourceDir = Join-Path $root 'docs\livrables-officiels'
$word = $null
$wdFormatDocumentDefault = 16
$wdExportFormatPDF = 17
$wdDoNotSaveChanges = 0

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0

    Get-ChildItem -Path $sourceDir -Filter *.html | Sort-Object Name | ForEach-Object {
        $htmlPath = $_.FullName
        $docxPath = [System.IO.Path]::ChangeExtension($htmlPath, '.docx')
        $pdfPath = [System.IO.Path]::ChangeExtension($htmlPath, '.pdf')

        Write-Host "Conversion de $($_.Name)"
        $document = $word.Documents.Open($htmlPath, $false, $true)
        try {
            $document.SaveAs([ref]$docxPath, [ref]$wdFormatDocumentDefault)
            $document.ExportAsFixedFormat($pdfPath, $wdExportFormatPDF)
        }
        finally {
            $document.Close($wdDoNotSaveChanges)
        }
    }
}
finally {
    if ($word -ne $null) {
        $word.Quit()
    }
}
