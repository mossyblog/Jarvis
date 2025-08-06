# WSL/Windows Networking Solution

This project runs in a mixed WSL/Windows environment where:
- **Frontend (Vite)**: Runs in WSL
- **Backend (API)**: Runs in Windows
- **Browser**: Can access from both WSL and Windows

## The Problem

WSL and Windows have different networking contexts:
- `localhost` in WSL != `localhost` in Windows
- WSL can't directly access Windows `localhost:5173`
- Windows can't directly access WSL services via `localhost`

## The Solution

We've implemented dynamic IP detection that:

1. **Auto-detects Windows host IP** from WSL using `ip route`
2. **Updates configuration files** automatically
3. **Works across different network setups**

## Files Modified

- `vite.config.ts` - Dynamic proxy target configuration
- `.env` - Dynamic API URL configuration
- `scripts/get-windows-host.js` - IP detection and config update script

## Usage

### Automatic (Recommended)
The system automatically detects the Windows host IP when starting Vite.

### Manual Fix
If networking breaks, run:
```bash
npm run fix-wsl
```

This will:
- Detect current Windows host IP
- Update `.env` with correct `VITE_API_URL`
- Update `vite.config.ts` proxy target
- Show current configuration

### Current Configuration
- **Windows Host IP**: `172.27.112.1` (auto-detected)
- **Frontend URL**: `http://0.0.0.0:5173` (accessible from Windows)
- **API Proxy**: `http://172.27.112.1:7071/api`
- **HMR**: Uses `0.0.0.0:5173` for cross-platform compatibility

## Accessing the App

- **From WSL**: `http://0.0.0.0:5173`
- **From Windows**: `http://localhost:5173` or `http://172.27.126.128:5173`
- **From Network**: `http://[WSL-IP]:5173`

## Troubleshooting

If you see connection issues:

1. **Run the fix script**: `npm run fix-wsl`
2. **Restart Vite server**: `npm run dev`
3. **Check Windows Firewall**: Ensure port 7071 is allowed
4. **Verify WSL networking**: `ping 172.27.112.1` should work

## Technical Details

The solution uses:
- **Dynamic IP detection**: Parses `ip route` output in WSL
- **Environment variables**: For runtime configuration
- **Vite proxy**: Routes `/api` calls to Windows host
- **Host binding**: `0.0.0.0` allows external access

This ensures seamless networking regardless of IP changes or different development environments.