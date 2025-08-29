# FolderSnippets

A Windows tray app that lets you quickly search your folder of text/markdown files, pick a snippet, and paste it into the active application. Pasting uses AutoHotkey.Interop to reliably send Ctrl+V.

## Requirements
- Windows 10/11 (x64)
- .NET 8 SDK
- Visual Studio 2022 or `dotnet` CLI

## Build
- Visual Studio: open `FolderSnippets.sln`, build Release x64
- CLI: `dotnet build -c Release`

## Run
- Launch the app; it lives in the system tray
- Global hotkey: `Ctrl+Alt+F`
- Pick a snippet; it is copied to the clipboard and pasted via AutoHotkey.Interop (Ctrl+V)

## Notes
- Uses the `AutoHotkey.Interop` NuGet package, which self-deploys the required AutoHotkey DLL
- Clipboard is restored after paste

Simon thanks GPT5 on Codex CLI for this code. Well done OpenAI team.
