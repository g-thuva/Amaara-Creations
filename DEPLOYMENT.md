# GitHub Pages Deployment Guide

This guide will help you deploy your Amaara Creations React application to GitHub Pages.

## Prerequisites

- GitHub repository (already set up)
- Node.js and npm installed
- Git configured

## Deployment Methods

### Method 1: Automatic Deployment (Recommended)

The project is configured with GitHub Actions for automatic deployment:

1. **Push to main branch**: The workflow will automatically trigger when you push changes to the main branch
2. **Check Actions tab**: Go to your GitHub repository → Actions tab to see the deployment progress
3. **Access your site**: Once deployed, your site will be available at:
   ```
   https://yourusername.github.io/Amaara-Creations/
   ```

### Method 2: Manual Deployment

If you prefer manual deployment:

1. **Build the project**:
   ```bash
   cd client
   npm run build
   ```

2. **Deploy to GitHub Pages**:
   ```bash
   npm run deploy
   ```

## Configuration Details

### Files Modified for GitHub Pages:

1. **`client/vite.config.js`**: Updated base path to `/Amaara-Creations/`
2. **`client/package.json`**: Added deploy scripts using gh-pages
3. **`.github/workflows/deploy.yml`**: GitHub Actions workflow for automatic deployment

### Important Notes:

- The base path is set to `/Amaara-Creations/` to match your repository name
- If you change your repository name, update the base path in `vite.config.js`
- The deployment uses the `gh-pages` branch to host your site
- All static assets will be properly referenced with the correct base path

## Troubleshooting

### Common Issues:

1. **404 Error on Routes**: Make sure you're using React Router's `HashRouter` instead of `BrowserRouter` for GitHub Pages
2. **Assets Not Loading**: Verify the base path in `vite.config.js` matches your repository name
3. **Build Fails**: Check the Actions tab for detailed error logs

### Checking Deployment Status:

1. Go to your repository on GitHub
2. Click on the "Actions" tab
3. Look for the "Deploy to GitHub Pages" workflow
4. Click on the latest run to see detailed logs

## Custom Domain (Optional)

To use a custom domain:

1. Create a `CNAME` file in the `client/public` folder with your domain
2. Configure your domain's DNS to point to `yourusername.github.io`
3. Enable custom domain in GitHub Pages settings

## Local Testing

To test the production build locally:

```bash
cd client
npm run build
npm run preview
```

This will serve the built files locally so you can verify everything works before deployment.

## Support

If you encounter any issues:
1. Check the GitHub Actions logs
2. Verify all configuration files are correct
3. Ensure your repository is public (required for free GitHub Pages)
