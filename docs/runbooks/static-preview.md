# GitHub Pages Deployment Guide

This guide will help you deploy your Amaara Creations React application to GitHub Pages.

## Local-only development and deployment readiness

The `Validate and deploy GitHub Pages` workflow always runs backend tests, frontend lint/type checks/tests, and Pages configuration policy tests. Public build and deployment run only when all four repository Actions variables are configured:

| Variable | Required value |
| --- | --- |
| `VITE_PUBLIC_SITE_URL` | Approved HTTPS storefront URL, including `/Amaara-Creations/`. |
| `VITE_API_BASE_URL` | Deployed public HTTPS API base ending in `/api/v1`. |
| `VITE_MEDIA_BASE_URL` | Deployed public HTTPS media base. |
| `VITE_MANAGEMENT_LOGIN_URL` | Approved HTTPS management login on a separate origin. |

When the API and admin run only on localhost, leave their public variables unset. The configuration job lists missing variables in its summary and skips the public build/deployment. Normal application checks continue. Existing published content is not replaced by this skipped run.

Complete but invalid settings still fail validation. The publishing build also runs the strict `python scripts/check-pages-config.py` guard, which rejects localhost, example hosts and invalid URL/routing combinations. The readiness flag never permits publishing an incomplete configuration.

After hosting the API, media and management application publicly, configure their real URLs and use **Run workflow** or push to `main`. GitHub Pages hosts the storefront files; the backend job builds and tests .NET without deploying the API or SQL Server. Follow the [staff deployment and cookie requirements](../security/ADMIN_ISOLATION_DEPLOYMENT.md) before enabling public sign-in.

## Prerequisites

- GitHub repository (already set up)
- Node.js and npm installed
- Git configured

## Deployment Methods

### Method 1: Automatic Deployment (Recommended)

The project is configured with GitHub Actions for automatic deployment:

1. **Enable GitHub Pages**: 
   - Go to your repository settings
   - Navigate to "Pages" section
   - Under "Source", select "GitHub Actions"
2. **Configure public URLs**: Set the four repository variables above when public hosting is ready.
3. **Run the workflow**: Push to `main` or run `Validate and deploy GitHub Pages` manually. Missing URLs skip publishing while validation continues.
4. **Check Actions tab**: Go to your GitHub repository → Actions tab to see validation and deployment results.
5. **Access your site**: Once deployed, your site will be available at:
   ```
   https://yourusername.github.io/Amaara-Creations/
   ```

### Method 2: Manual Deployment

If you prefer manual deployment:

1. **Build the project**:
   ```bash
   cd client
   python ../scripts/check-pages-config.py
   npm run build
   ```

2. **Deploy to GitHub Pages**:
   ```bash
   npm run deploy
   ```

## Configuration Details

### Files Modified for GitHub Pages:

1. **`client/vite.config.js`**: Reads the public base path from `VITE_PUBLIC_BASE_PATH`.
2. **`client/package.json`**: Added deploy scripts using gh-pages
3. **`.github/workflows/pages.yml`**: GitHub Actions validation, configuration readiness and optional deployment.

### Important Notes:

- The base path is set to `/Amaara-Creations/` to match your repository name
- If you change your repository name, update the base path in `vite.config.js`
- The deployment uses GitHub's new Pages deployment system (no separate branch needed)
- All static assets will be properly referenced with the correct base path

## Troubleshooting

### Common Issues:

1. **404 Error on Routes**: Make sure you're using React Router's `HashRouter` instead of `BrowserRouter` for GitHub Pages
2. **Assets Not Loading**: Verify the base path in `vite.config.js` matches your repository name
3. **Build Fails**: Check the Actions tab for detailed error logs

### Checking Deployment Status:

1. Go to your repository on GitHub
2. Click on the "Actions" tab
3. Look for the "Validate and deploy GitHub Pages" workflow
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
