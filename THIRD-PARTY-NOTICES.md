# Third-party notices

PlotView itself is released under the MIT License (see `LICENSE`).
The installer also redistributes the following components, each under its own license.

| Component | Used for | License | Source |
|---|---|---|---|
| PDFium | PDF rendering engine (`pdfium.dll`) | BSD-3-Clause / Apache-2.0 | https://pdfium.googlesource.com/pdfium/ |
| pdfium-binaries (bblanchon) | Prebuilt PDFium binaries and NuGet package | MIT (packaging scripts); PDFium's own licenses apply to the binaries | https://github.com/bblanchon/pdfium-binaries |
| .NET Runtime and Windows Desktop Runtime | Application runtime (bundled, self-contained) | MIT | https://github.com/dotnet/runtime |
| Inno Setup | Builds the installer (not redistributed as a library) | Inno Setup License | https://jrsoftware.org/isinfo.php |

PDFium includes further third-party code (FreeType, libjpeg-turbo, OpenJPEG, zlib, lcms2, and others).
The full license texts for the PDFium build are published with each release of pdfium-binaries
in its `LICENSE` file and `licenses/` folder.
