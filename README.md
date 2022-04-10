# BrusselBot
A Discord music bot for my online friendos.

Latest Build Version: **4-8-2022.0**

# Can I use this bot for my own purposes?
Sure! Feel free to follow the steps below and use it however you wish. You can feel free to modify the bot however you see fit.

# Running the Bot
- Download the **LatestBuild** folder and the **Lavalink** folder along with **lavalink.bat** and **bot.bat** or just clone the project.
- Create a bot account by following [this page.](https://dsharpplus.github.io/articles/basics/bot_account.html)
- Inside of **LatestBuild/netcoreapp3.1**, complete **settings.xml** with the settings you want.
  - Most importantly, include your bot token from the previous step!
  - Also ensure that your Lavalink settings in **settings.xml** match with the settings inside of **Lavalink/application.yml**.
- Run **lavalink.bat**.
- Run **bot.bat** or **LatestBuild/netcoreapp3.1/BrusselMusicBot.exe** — either works!

# Discord Commands
- **join**: Causes the bot to join the user's current channel.
- **leave**: Causes the bot to leave its current channel.
- **play \<search string\>**: Searches for a track using the given string and adds it to the queue. If the bot is not already in the user's channel, this command causes it to join.
- **skip**: Skips to the end of the currently playing track.
- **playskip \<search string\>**: Searches for a track using the given string and adds it to the front of the queue, then skips to the end of the currently playing track.
- **skip**: Skips to the end of the currently playing track.
- **pause**: Pauses or unpauses the bot.
- **loop**: Starts/stops looping the current track.
- **seek \<timestamp in seconds (eg. 75)\>**: Skips to the specified playback position in the track.
- **np ("Now Playing")**: Displays the current track and playback position.
- **queue**: Displays the current queue of tracks.
- **search**: Get the first 5 results and list them, then wait for the user to pick one.
- **volume**: Set the volume. Values range from 0-100.

# Console Commands
- **help**: Lists all commands.
- **settings**: Displays all of the settings the bot is currently using.
- **version**: Displays the current bot version.
- **getConns**: Lists all active channel connections.




