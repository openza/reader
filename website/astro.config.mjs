import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import sitemap from '@astrojs/sitemap';

export default defineConfig({
  site: 'https://openza.github.io',
  base: '/reader',
  integrations: [
    sitemap(),
    starlight({
      title: 'Openza Reader',
      description: 'Documentation for Openza Reader - A native WinUI 3 Markdown reader for Windows',
      logo: {
        src: './src/assets/logo.svg',
      },
      social: {
        github: 'https://github.com/openza/reader',
      },
      customCss: [
        './src/styles/custom.css',
      ],
      sidebar: [
        {
          label: 'Getting Started',
          items: [
            { label: 'Introduction', slug: 'getting-started/introduction' },
            { label: 'Installation', slug: 'getting-started/installation' },
            { label: 'Reading Markdown', slug: 'getting-started/reading-markdown' },
          ],
        },
        {
          label: 'Features',
          items: [
            { label: 'Markdown Rendering', slug: 'features/markdown-rendering' },
            { label: 'Navigation and Search', slug: 'features/navigation-search' },
            { label: 'Security Model', slug: 'features/security-model' },
          ],
        },
        {
          label: 'Development',
          items: [
            { label: 'Building from Source', slug: 'development/building' },
            { label: 'Architecture', slug: 'development/architecture' },
            { label: 'Contributing', slug: 'development/contributing' },
          ],
        },
      ],
    }),
  ],
});
