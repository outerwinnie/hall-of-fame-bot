# 🤖 Hall of Fame with Google Drive CSV Integration 📊

Welcome to the **Hall of Fame** project! This bot fetches messages and images from a CSV file stored in Google Drive and posts them to a specified Discord channel.

## 🌟 Features

- **Fetch Data from Google Drive**: The bot reads a CSV file stored on Google Drive.
- **Message Filtering**: Only fetches and posts messages with **8 or more reactions**. 
- **Flexible Posting**: Posts messages to a Discord channel at a specific time every day. 🕒
- **Custom Message Format**: Outputs messages in the format: `name + message + message_link + image_url (if available)`.

## 🚀 Getting Started

### 1. Prerequisites

Before running the bot, make sure you have the following:

- **.NET Core SDK** installed.
- **Google Cloud Platform** account with a project that has access to Google Drive API.
- **Discord Bot Token**: You can create one by following the [Discord Developer Portal](https://discord.com/developers/applications).
- **CSV File on Google Drive** containing your data.

### 2. Environment Variables

Set the following environment variables to configure your bot:

```plaintext
DISCORD_BOT_TOKEN=your-discord-bot-token
DISCORD_CHANNEL_ID=your-discord-channel-id
GOOGLE_DRIVE_FILE_ID=your-google-drive-file-id
GOOGLE_CREDENTIALS_PATH=path-to-your-google-credentials.json
POST_TIME=HH:mm:ss (time in Spain to post)
```

### 3. CSV File Structure

Your CSV file should have the following columns:

- **name**: The name of the person or bot.
- **message**: The content of the message.
- **message_link**: A link to the original message.
- **image_url**: (Optional) A link to the image.
- **has_spoilers**: Whether the message contains spoilers.
- **total_reactions**: The number of reactions.


## 🛠️ Customization

You can easily modify the bot's behavior by editing the `Program.cs` file:

- **Posting Time**: Change the `POST_TIME` to adjust when the bot posts.
- **Message Format**: Modify the message format in the code to suit your needs.
