# Relate Mail Mobile App

React Native mobile application for Relate Mail, built with Expo.

## Prerequisites

- Node.js 18+
- npm or yarn
- Expo CLI (`npm install -g expo-cli`)
- For iOS development: Xcode (macOS only)
- For Android development: Android Studio with SDK

## Getting Started

1. Install dependencies:

```bash
npm install
```

2. Start the development server:

```bash
npm start
```

3. Run on a specific platform:

```bash
# Android
npm run android

# iOS
npm run ios

# Web
npm run web
```

## Project Structure

```
mobile/
  app/                    # Expo Router pages (file-based routing)
    (auth)/               # Authentication flow screens
    (main)/               # Main app screens (requires account)
      (tabs)/             # Tab navigation (inbox, sent, settings)
      emails/             # Email detail screens
  components/             # Reusable components
    ui/                   # Base UI components (Button, Input, Card, etc.)
    mail/                 # Email-specific components
  lib/                    # Core libraries
    api/                  # API client, hooks, types
    auth/                 # Account store, OIDC, secure storage
```

## Features

- **Multi-Account Support**: Connect to multiple Relate servers
- **OIDC Authentication**: Secure login with API key persistence
- **Email Management**: View inbox, sent mail, search emails
- **Swipe Actions**: Swipe to delete or mark read/unread
- **Real-time Updates**: SignalR integration for live email notifications
- **Responsive Design**: Works on phones, tablets, and desktop

## Tech Stack

- **Framework**: Expo SDK 52 (managed workflow)
- **Navigation**: Expo Router v4
- **Styling**: NativeWind (Tailwind CSS for React Native)
- **State Management**: TanStack Query + Zustand
- **Authentication**: expo-auth-session with PKCE
- **Secure Storage**: expo-secure-store

## Configuration

The app automatically discovers server capabilities when you add an account.
No environment variables are needed for the mobile app itself.

## Building for Production

```bash
# Build for Android
npm run build:android

# Build for iOS
npm run build:ios
```

See [Expo EAS Build documentation](https://docs.expo.dev/build/introduction/) for more details.
