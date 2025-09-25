# 🔑 Unity CI/CD Secrets Setup Guide

**Current Status**: ❌ Unity licensing not configured
**Error**: `Missing Unity License File and no Serial was found`

## 📋 Required Actions

### **Step 1: Choose Your License Type**

#### For Personal (Free) Unity License:
1. **Open Unity Hub** on your computer
2. **Sign in** to your Unity account
3. **Activate Personal License** (if not already done)
4. **Find your license file** (.ulf):
   - **Windows**: `C:\ProgramData\Unity\Unity_lic.ulf`
   - **macOS**: `/Library/Application Support/Unity/Unity_lic.ulf`
   - **Linux**: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

#### For Professional Unity License:
1. **Go to Unity Dashboard**: https://id.unity.com/
2. **Navigate**: Organizations → Your Org → Subscriptions
3. **Copy your serial key** (starts with numbers/letters)

### **Step 2: Add Secrets to GitHub**

1. **Go to your GitHub repository**
2. **Click**: Settings → Secrets and variables → Actions
3. **Click**: "New repository secret"

#### For Personal License - Add these 3 secrets:
```
Name: UNITY_LICENSE
Value: [Paste ENTIRE contents of your .ulf file - NOT base64 encoded]

Name: UNITY_EMAIL
Value: [Your Unity account email]

Name: UNITY_PASSWORD
Value: [Your Unity account password]
```

#### For Professional License - Add these 3 secrets:
```
Name: UNITY_SERIAL
Value: [Your Unity serial key]

Name: UNITY_EMAIL
Value: [Your Unity account email]

Name: UNITY_PASSWORD
Value: [Your Unity account password]
```

### **Step 3: Test Your Setup**

1. **Go to**: Actions tab in your GitHub repository
2. **Run**: "Unity CI Diagnostic" workflow manually
3. **Check results**: It will tell you exactly what's configured

## 🚨 Important Notes

- **DO NOT** set both `UNITY_LICENSE` and `UNITY_SERIAL` - choose one method
- **UNITY_LICENSE** should contain the raw text from .ulf file (not base64)
- **Keep your credentials secure** - never commit them to code
- **Personal licenses** are tied to your Unity ID and have usage limitations

## 🔍 Troubleshooting

**❌ Can't find .ulf file?**
- Run Unity Hub → Preferences → License Management
- If no license shown, activate one first
- The file is created when you activate a license

**❌ Still getting license errors?**
- Check secret names are EXACT (case-sensitive)
- Verify Unity email/password are correct
- Try removing and re-adding the secrets

**❌ Professional license not working?**
- Make sure you're using the serial key, not license file
- Verify the serial is active in Unity Dashboard
- Check organization permissions

## 📞 Need Help?

Run the diagnostic workflow first - it will show you exactly what's missing:

1. GitHub repo → Actions → "Unity CI Diagnostic" → Run workflow
2. Check the output for specific missing secrets
3. Follow the setup steps above for your license type

---
**After setup**: Your Unity CI/CD should work without licensing errors! 🎉