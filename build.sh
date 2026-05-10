#!/bin/bash
set -e

echo "=== Building Frontend ==="
cd Frontend
npm ci
npm run build
cd ..

echo "=== Copying frontend dist to backend wwwroot ==="
rm -rf Backend/YdcViewer.Api/wwwroot
cp -r Frontend/dist Backend/YdcViewer.Api/wwwroot

echo "=== Building Backend ==="
cd Backend
dotnet publish YdcViewer.Api -c Release -o ../publish
cd ..

echo ""
echo "=== Build Complete ==="
echo "Run:  cd publish && dotnet YdcViewer.Api.dll"
echo "Open: http://localhost:5000"
