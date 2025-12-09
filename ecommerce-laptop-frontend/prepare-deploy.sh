#!/bin/bash

# Script to prepare for Vercel deployment
echo " Cleaning up for deployment..."

# Remove lockfiles and node_modules
echo "Removing old lockfiles and node_modules..."
rm -f package-lock.json
rm -f pnpm-lock.yaml
rm -rf node_modules

# Install with npm
echo "Installing dependencies with npm..."
npm install

# Build to test
echo "Building project..."
npm run build

echo " Project ready for deployment!"
echo " You can now deploy to Vercel"
