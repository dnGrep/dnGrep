rem remove files from the build output that are in the wrong location or not part of the zip artifact
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\dnGREP.Engines.MsWord.plugin" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\dnGREP.Engines.MsWord.plugin"
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\dnGREP.Engines.OpenXml.plugin" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\dnGREP.Engines.OpenXml.plugin"
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\dnGREP.Engines.Pdf.plugin" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\dnGREP.Engines.Pdf.plugin"
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\pdftotext.exe" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\pdftotext.exe"
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\pdf-readme" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\pdf-readme"
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\word-readme" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\word-readme"
if exist "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\ICSharpCode.AvalonEdit.xml" del "%APPVEYOR_BUILD_FOLDER%\dnGREP.WPF\bin\%CONFIGURATION%\ICSharpCode.AvalonEdit.xml"
