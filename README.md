# GovernmentWatch_Epstein (JeffView)

## Purpose
The primary purpose of this project is to make the Epstein files more accessible to all people. By providing an easy-to-use, streamlined application, we aim to demonstrate to all citizens a blueprint for making vital public information easily accessible, fostering a more informed society. The application features an integrated document viewer, intuitive navigation, and automated session handling to seamlessly retrieve and display data sets directly from government sources.

## Current Status
**Open Source C# .NET 9 with a standalone compiled executable.**

The current implementation is a Windows Desktop application built using WPF and modern .NET, leveraging WebView2 for robust, authenticated session management.

## Features
- **Direct Library Access:** Automatically browse and load datasets 1 through 12.
- **Automated Authentication:** Seamlessly handles the age verification interstitial via an embedded browser session.
- **High-Quality Export:** Convert any loaded document into Jpeg, Png, Tiff, or Bmp image formats with adjustable compression.
- **Modern UI:** Built with MaterialDesignThemes for a clean, dark-mode aesthetic.

## Architecture & Implementation Details
This application utilizes a unified browser-based viewport (WebView2) to manage complex authentication requirements. Public repositories often employ session tokens, cookies, and interactive interstitials (like age verification) to deter automated access. 

By operating within a persistent browser session, this app automatically executes a script to bypass the age-verification gate and inherits the necessary cookies. It then utilizes this authenticated context to fetch and render the PDF documents directly. This approach bypasses traditional `HttpClient` limitations and provides a reliable, open-source blueprint for accessing public records hidden behind interactive gateways.

## Call to Action
We need your help to expand this initiative! We strongly encourage developers to fork, refactor, and port this application across multiple frameworks to cover all modern operating systems, including macOS, Linux, iOS, and Android. 

Additionally, we need assistance with distribution and promotion across all channels:
- Share the compiled binaries and source code.
- Discuss and promote the project on social media, websites, forums, and blogs.
- Help us build a more transparent and informed society.

## License
Free with Credit to the original writer: **radar624mtd**
