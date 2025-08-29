# FolderSnippets

A small Windows app that lives in your system tray and lets you paste saved snippets into any program in a couple of keystrokes.

Download: [Download](https://github.com/MrSimonC/LLM-Prompt-Injector/raw/refs/heads/master/Binary/FolderSnippets.exe) (Windows 64‑bit)

How it works
- Press Ctrl+Alt+F to open the search box.
- Type to find a snippet from your chosen folder of .txt/.md files.
- Press Enter to paste it into the app you’re using.

Getting started
- Run FolderSnippets.exe.
- Choose the folder that holds your snippets.
- Use Ctrl+Alt+F whenever you want to paste one.

Notes
- Your clipboard is restored after each paste.
- You can enable “Start on Windows login” from the tray menu.

Icon
- Place your PNG at `Assets/icon.png` and create the app icon with ImageMagick:
  - Install: `winget install ImageMagick.ImageMagick`
  - Convert: `magick Assets\icon.png -define icon:auto-resize=256,128,64,48,32,24,20,16 Assets\app.ico`
- The app automatically uses `Assets/app.ico` for the tray and window icons.

Simon thanks GPT5 on Codex CLI for this code. Well done OpenAI team.
