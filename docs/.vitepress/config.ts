import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Relate Mail',
  description: 'Documentation for the Relate Mail full-stack email platform',
  base: '/',

  head: [
    ['link', { rel: 'icon', href: '/logo.svg' }],
  ],

  themeConfig: {
    logo: '/logo.svg',

    nav: [
      { text: 'Getting Started', link: '/getting-started/' },
      { text: 'Architecture', link: '/architecture/' },
      {
        text: 'Platform',
        items: [
          { text: 'Backend', link: '/backend/' },
          { text: 'Frontend', link: '/frontend/' },
        ],
      },
      { text: 'API Reference', link: '/api-reference/' },
      { text: 'Infrastructure', link: '/infrastructure/' },
    ],

    sidebar: {
      '/getting-started/': [
        {
          text: 'Getting Started',
          items: [
            { text: 'Overview', link: '/getting-started/' },
            { text: 'Installation', link: '/getting-started/installation' },
            { text: 'Local Development', link: '/getting-started/local-development' },
            { text: 'Docker Quickstart', link: '/getting-started/docker-quickstart' },
          ],
        },
      ],

      '/architecture/': [
        {
          text: 'Architecture',
          items: [
            { text: 'Overview', link: '/architecture/' },
            { text: 'Monorepo Structure', link: '/architecture/monorepo-structure' },
            { text: 'Data Flow', link: '/architecture/data-flow' },
          ],
        },
      ],

      '/backend/': [
        {
          text: 'Backend',
          link: '/backend/',
          items: [
            {
              text: 'REST API',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/backend/api/' },
                { text: 'Controllers', link: '/backend/api/controllers' },
                { text: 'Authentication', link: '/backend/api/authentication' },
                { text: 'SignalR Hub', link: '/backend/api/signalr-hub' },
                { text: 'Services', link: '/backend/api/services' },
                { text: 'Health Checks', link: '/backend/api/health-checks' },
              ],
            },
            {
              text: 'SMTP Server',
              collapsed: true,
              items: [
                { text: 'Overview', link: '/backend/smtp-server/' },
                { text: 'Message Handling', link: '/backend/smtp-server/message-handling' },
                { text: 'TLS Configuration', link: '/backend/smtp-server/tls-configuration' },
                { text: 'Health Checks', link: '/backend/smtp-server/health-checks' },
              ],
            },
            {
              text: 'POP3 Server',
              collapsed: true,
              items: [
                { text: 'Overview', link: '/backend/pop3-server/' },
                { text: 'Protocol', link: '/backend/pop3-server/protocol' },
                { text: 'Handlers', link: '/backend/pop3-server/handlers' },
              ],
            },
            {
              text: 'IMAP Server',
              collapsed: true,
              items: [
                { text: 'Overview', link: '/backend/imap-server/' },
                { text: 'Protocol', link: '/backend/imap-server/protocol' },
                { text: 'Handlers', link: '/backend/imap-server/handlers' },
              ],
            },
            {
              text: 'Core (Domain)',
              collapsed: true,
              items: [
                { text: 'Overview', link: '/backend/core/' },
                { text: 'Entities', link: '/backend/core/entities' },
                { text: 'Interfaces', link: '/backend/core/interfaces' },
                { text: 'Models', link: '/backend/core/models' },
                { text: 'Protocol Utilities', link: '/backend/core/protocol' },
              ],
            },
            {
              text: 'Infrastructure',
              collapsed: true,
              items: [
                { text: 'Overview', link: '/backend/infrastructure/' },
                { text: 'Data Access', link: '/backend/infrastructure/data-access' },
                { text: 'Repositories', link: '/backend/infrastructure/repositories' },
                { text: 'Services', link: '/backend/infrastructure/services' },
                { text: 'Health Checks', link: '/backend/infrastructure/health-checks' },
                { text: 'Telemetry', link: '/backend/infrastructure/telemetry' },
              ],
            },
          ],
        },
      ],

      '/frontend/': [
        {
          text: 'Frontend',
          link: '/frontend/',
          items: [
            {
              text: 'Web App',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/frontend/web/' },
                { text: 'Routing', link: '/frontend/web/routing' },
                { text: 'Components', link: '/frontend/web/components' },
                { text: 'API Layer', link: '/frontend/web/api-layer' },
                { text: 'SignalR Integration', link: '/frontend/web/signalr-integration' },
                { text: 'Authentication', link: '/frontend/web/authentication' },
                { text: 'Testing', link: '/frontend/web/testing' },
              ],
            },
            {
              text: 'Mobile App',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/frontend/mobile/' },
                { text: 'Navigation', link: '/frontend/mobile/navigation' },
                { text: 'Authentication', link: '/frontend/mobile/authentication' },
                { text: 'Security', link: '/frontend/mobile/security' },
                { text: 'Components', link: '/frontend/mobile/components' },
                { text: 'Testing', link: '/frontend/mobile/testing' },
              ],
            },
            {
              text: 'Desktop App',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/frontend/desktop/' },
                { text: 'Rust Backend', link: '/frontend/desktop/rust-backend' },
                { text: 'Features', link: '/frontend/desktop/features' },
                { text: 'Building', link: '/frontend/desktop/building' },
              ],
            },
            {
              text: 'Shared Package',
              collapsed: true,
              items: [
                { text: 'Overview', link: '/frontend/shared/' },
                { text: 'Types', link: '/frontend/shared/types' },
                { text: 'Components', link: '/frontend/shared/components' },
                { text: 'Utilities', link: '/frontend/shared/utilities' },
              ],
            },
          ],
        },
      ],

      '/api-reference/': [
        {
          text: 'API Reference',
          items: [
            { text: 'Overview', link: '/api-reference/' },
            { text: 'Emails', link: '/api-reference/emails' },
            { text: 'Outbound Email', link: '/api-reference/outbound' },
            { text: 'Labels', link: '/api-reference/labels' },
            { text: 'Filters', link: '/api-reference/filters' },
            { text: 'Profile', link: '/api-reference/profile' },
            { text: 'Preferences', link: '/api-reference/preferences' },
            { text: 'SMTP Credentials', link: '/api-reference/smtp-credentials' },
            { text: 'Push Subscriptions', link: '/api-reference/push-subscriptions' },
            { text: 'Discovery', link: '/api-reference/discovery' },
            { text: 'Config', link: '/api-reference/config' },
            { text: 'Internal Notifications', link: '/api-reference/notifications' },
            { text: 'SignalR (Real-time)', link: '/api-reference/signalr' },
          ],
        },
      ],

      '/infrastructure/': [
        {
          text: 'Infrastructure',
          link: '/infrastructure/',
          items: [
            {
              text: 'Docker',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/infrastructure/docker/' },
                { text: 'Dockerfile', link: '/infrastructure/docker/dockerfile' },
                { text: 'Compose Files', link: '/infrastructure/docker/compose-files' },
                { text: 'Health Checks', link: '/infrastructure/docker/health-checks' },
              ],
            },
            {
              text: 'CI/CD',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/infrastructure/ci-cd/' },
                { text: 'CI Workflow', link: '/infrastructure/ci-cd/ci-workflow' },
                { text: 'Docker Publish', link: '/infrastructure/ci-cd/docker-publish' },
                { text: 'Mobile Build', link: '/infrastructure/ci-cd/mobile-build' },
                { text: 'Desktop Build', link: '/infrastructure/ci-cd/desktop-build' },
                { text: 'Security Scanning', link: '/infrastructure/ci-cd/security-scanning' },
              ],
            },
            {
              text: 'Configuration',
              collapsed: false,
              items: [
                { text: 'Overview', link: '/infrastructure/configuration/' },
                { text: 'Environment Variables', link: '/infrastructure/configuration/environment-variables' },
                { text: 'App Settings', link: '/infrastructure/configuration/appsettings' },
                { text: 'Email Client Setup', link: '/infrastructure/configuration/email-client-setup' },
              ],
            },
          ],
        },
      ],

      '/testing/': [
        {
          text: 'Testing',
          items: [
            { text: 'Overview', link: '/testing/' },
            { text: 'Unit Tests', link: '/testing/unit-tests' },
            { text: 'Integration Tests', link: '/testing/integration-tests' },
            { text: 'E2E Tests', link: '/testing/e2e-tests' },
            { text: 'Test Common', link: '/testing/test-common' },
          ],
        },
      ],

      '/contributing/': [
        {
          text: 'Contributing',
          items: [
            { text: 'Overview', link: '/contributing/' },
            { text: 'Development Setup', link: '/contributing/development-setup' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/four-robots/relate-mail' },
    ],

    editLink: {
      pattern: 'https://github.com/four-robots/relate-mail/edit/main/docs/:path',
      text: 'Edit this page on GitHub',
    },

    search: {
      provider: 'local',
    },

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright Four Robots',
    },
  },
})
