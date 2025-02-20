<div align="center">

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

</div>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">WebPageHost</h1>

  <p align="center">
    Simple Windows Command line (CLI) tool to open URLs in WebView2 (Microsoft Edge web browser control).
    <br />
    <a href="https://github.com/thgossler/WebPageHost/issues">Report Bug</a>
    ·
    <a href="https://github.com/thgossler/WebPageHost/issues">Request Feature</a>
    ·
    <a href="https://github.com/thgossler/WebPageHost#contributing">Contribute</a>
    ·
    <a href="https://github.com/sponsors/thgossler">Sponsor project</a>
    ·
    <a href="https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J">Sponsor via PayPal</a>
  </p>
</div>

## About The Project

This is a Windows simple command line interface (CLI) tool for opening web page URLs in an embedded Microsoft WebView2 control. It has a variety of options to control the behavior of the window and the embedded web browser. Further, it supports customization of the output via JavaScript which allows, for example, to use it for letting the user select anything from the web page and return it as result on standard output.

[![WebPageHost Screen Shot #1][product-screenshot]]([https://github.com/thgossler/WebPageHost/])

[![WebPageHost Screen Shot #2][product-screenshot2]]([https://github.com/thgossler/WebPageHost/])

> _**Note:** This tool was written by me in my spare time and will be developed only sporadically._

### Built With

* [.NET 9 (C#)](https://dotnet.microsoft.com/en-us/)
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

1. Download the [self-contained single executable file]([https://github.com/thgossler/WebPageHost/releases/download/v1.0.3/WebPageHost.exe) from the [releases](https://github.com/thgossler/WebPageHost/releases) section

2. Copy it to a location where you can easily call it, perhaps in a folder which is in your PATH environment variable

3. Open a command prompt or PowerShell and type `WebPageHost open --help`

4. Try the following simple example:

   ```shell
   WebPageHost open https://github.com/thgossler/WebPageHost#readme
   ```

5. Try the following more complicated example:

   ```shell
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

## Donate

If you are using the tool but are unable to contribute technically, please consider promoting it and donating an amount that reflects its value to you. You can do so either via PayPal

[![Donate via PayPal](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J)

or via [GitHub Sponsors](https://github.com/sponsors/thgossler).

## License

Distributed under the MIT License. See [`LICENSE`](https://github.com/thgossler/WebPageHost/blob/main/LICENSE) for more information.

<!-- MARKDOWN LINKS & IMAGES (https://www.markdownguide.org/basic-syntax/#reference-style-links) -->
[contributors-shield]: https://img.shields.io/github/contributors/thgossler/WebPageHost.svg
[contributors-url]: https://github.com/thgossler/WebPageHost/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/thgossler/WebPageHost.svg
[forks-url]: https://github.com/thgossler/WebPageHost/network/members
[stars-shield]: https://img.shields.io/github/stars/thgossler/WebPageHost.svg
[stars-url]: https://github.com/thgossler/WebPageHost/stargazers
[issues-shield]: https://img.shields.io/github/issues/thgossler/WebPageHost.svg
[issues-url]: https://github.com/thgossler/WebPageHost/issues
[license-shield]: https://img.shields.io/github/license/thgossler/WebPageHost.svg
[license-url]: https://github.com/thgossler/WebPageHost/blob/main/LICENSE
[product-screenshot]: images/screenshot.jpg
[product-screenshot2]: images/screenshot2.jpg
