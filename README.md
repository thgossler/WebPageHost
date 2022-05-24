[![Issues][issues-badge]][issues-url]
[![MIT License][license-badge]][license-url]


# WebPageHost

Simple Windows Command line (CLI) tool to open URLs in WebView2 (Microsoft Edge web browser control).

[Report Bug](https://github.com/thgossler/WebPageHost/issues) · [Request Feature](https://github.com/thgossler/WebPageHost/issues)


## About The Project

This is a Windows simple command line interface (CLI) tool for opening web page URLs in an embedded Microsoft WebView2 control. It has a variety of options to control the behavior of the window and the embedded web browser. Further, it supports customization of the output via JavaScript.

[![Product Name Screen Shot][product-screenshot]](https://example.com)

> _**Note:** This tool was written by me in my spare time and will be developed only sporadically._


### Built With

* [.NET 6 (C#)](https://dotnet.microsoft.com/en-us/)
* [WinForms](https://github.com/dotnet/winforms)
* [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
* [Spectre Console Cli](https://github.com/spectreconsole/spectre.console)
* [InputSimulator.Core](https://github.com/cwevers/InputSimulatorCore)

> _**Note**: I would like to make this tool also available on Linux some day. I have chosen WinForms initially because it was the fasted way for me to put this tool together :smiley:, it is open source and is still supported by Microsoft. And, I still haven't given up hope that it will eventually be available on Linux including the WebView2 wrapper control. In case WPF or MAUI should be available on Linux earlier than WinForms, the source code should be migrated. Alternatively, [Avalonia UI](https://avaloniaui.net/) could be considered._


## Getting Started

### Prerequisites

* Latest .NET SDK
  ```sh
  winget install -e --id Microsoft.dotnet
  ```
* Microsoft Edge WebView2 (if not already installed with the operating system)<br/>
  https://go.microsoft.com/fwlink/p/?LinkId=2124703

### Installation as Tool for Use

1. Download the [self-contained single executable file](https://github.com/thgossler/WebPageHost/releases/download/v1.0.0/WebPageHost.exe) from the [releases](https://github.com/thgossler/WebPageHost/releases) section

2. Copy it to a location where you can easily call it, perhaps in a folder which is in your PATH environment variable

3. Open a command prompt or PowerShell and type `WebPageHost open --help`

4. Try the following simple example:
   ```
   WebPageHost open https://github.com/thgossler/WebPageHost#readme
   ```

5. Try the following more complicated example:
   ```
   WebPageHost open "https://github.com/trending?since=monthly&spoken_language_code=en" -z 0.7 -s 800x1024 -x "const regex = new RegExp('github.com\\/([^\\/]+\\/[^\\/]+)', 'gm'); let m = regex.exec(window.location.host+window.location.pathname); 'Selected GitHub project: '+(m !== null ? m[1] : 'none');" --ontop
   ```

### Installation from Source for Development

1. Clone the repo
   ```sh
   git clone https://github.com/thgossler/WebPageHost.git
   ```
2. Build
   ```sh
   dotnet build
   ```
3. Run without arguments to get help
   ```sh
   dotnet run
   ```

Alternatively, you can open the folder in [VS Code](https://code.visualstudio.com/) or the solution (.sln file) in the [Microsoft Visual Studio IDE](https://visualstudio.microsoft.com/vs/) and press F5.


## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star :wink: Thanks!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request


## License

Distributed under the MIT License. See [`LICENSE.txt`](https://github.com/thgossler/WebPageHost/LICENSE.txt) for more information.


## Contact

Thomas Gossler - [@thgossler](https://twitter.com/thgossler)<br/>
Project Link: [https://github.com/thgossler/WebPageHost](https://github.com/thgossler/WebPageHost)


<!-- See: https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[issues-badge]: https://img.shields.io/github/issues/thgossler/WebPageHost.svg?style=for-the-badge
[issues-url]: https://github.com/thgossler/WebPageHost/issues
[license-badge]: https://img.shields.io/github/license/thgossler/WebPageHost.svg?style=for-the-badge
[license-url]: https://github.com/thgossler/WebPageHost/blob/main/LICENSE.txt
[product-screenshot]: images/screenshot.png
