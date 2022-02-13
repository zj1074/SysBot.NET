# SysBot.Pokemon.QQ
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)
Most codes are based on [SysBot.Pokemon.Twitch](https://github.com/kwsch/SysBot.NET/tree/master/SysBot.Pokemon.Twitch)

Uses Mirai.Net as a dependency via Nuget.

## Support QQ by mirai.net:
- Support [ALM-Showdown-Sets](https://github.com/architdate/PKHeX-Plugins/wiki/ALM-Showdown-Sets)
- Support PK8 PB8 PA8 file upload

## Usage
- Build SysBot.Pokemon.ConsoleApp
- Download [MCL](https://github.com/iTXTech/mirai-console-loader) and add mirai plugin [mirai-api-http](https://github.com/project-mirai/mirai-api-http)
- Config mirai-api-http by [this](https://github.com/project-mirai/mirai-api-http/blob/master/README.md), get the VerifyKey of your config
- Start [MCL](https://github.com/iTXTech/mirai-console-loader) and check if it works
- Use the miraiQQconfig template and replace your QQ number and your trade QQ group number, save as miraiQQconfig.json to the folder of your `SysBot.Pokemon.ConsoleApp.exe`
- Run the `SysBot.Pokemon.ConsoleApp.exe`
- Send `$trade` and press `ctrl+enter` to the next line, then parse your ALM-Showdown-Sets before sending your QQ message
- Or you can upload a PK8/PB8/PA8 file directly

## miraiQQconfig template
```
{
  "Address": "localhost:8080",
  "QQ": "your QQ number",
  "VerifyKey": "INITKEY6Ipj3WbA",
  "GroupId": "trade QQ group number"
}
```
