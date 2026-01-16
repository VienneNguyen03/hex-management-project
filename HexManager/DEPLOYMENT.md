# Deployment Guide - HexManager

## 🚀 Recommended Hosting Options

### 1. **Railway** (Recommended - Easiest)
**Best for: Quick deployment, modern platform**

**Pros:**
- ✅ Very easy to deploy (connect GitHub repo)
- ✅ Supports .NET 8 out of the box
- ✅ Persistent storage for SQLite database
- ✅ Free tier available ($5 credit/month)
- ✅ Automatic HTTPS
- ✅ Environment variables support

**Steps:**
1. Push code to GitHub
2. Go to [railway.app](https://railway.app)
3. New Project → Deploy from GitHub
4. Select your repo
5. Add environment variables:
   - `ConnectionStrings__DefaultConnection` = `Data Source=/app/data/hexmanager.db`
   - `EmailSettings__SmtpServer` = `smtp.gmail.com`
   - `EmailSettings__SmtpPort` = `587`
   - `EmailSettings__SmtpUsername` = `your-email@gmail.com`
   - `EmailSettings__SmtpPassword` = `your-app-password`
   - `EmailSettings__FromEmail` = `your-email@gmail.com`
   - `Authentication__AuthorizedEmail` = `your-email@gmail.com`
6. Add persistent volume at `/app/data` for database
7. Deploy!

**Cost:** ~$5-10/month for small apps

---

### 2. **Azure App Service** (Microsoft Ecosystem)
**Best for: Enterprise, Microsoft integration**

**Pros:**
- ✅ Excellent .NET support
- ✅ Free tier available
- ✅ Easy deployment from Visual Studio/GitHub
- ✅ Auto-scaling
- ✅ Built-in monitoring

**Steps:**
1. Create Azure account
2. Create App Service (Linux, .NET 8)
3. Configure deployment from GitHub
4. Set Application Settings (environment variables)
5. For SQLite: Use Azure Files for persistent storage

**Cost:** Free tier available, ~$13/month for Basic tier

---

### 3. **Fly.io** (Best Performance)
**Best for: Global distribution, performance**

**Pros:**
- ✅ Great for .NET apps
- ✅ Global edge network
- ✅ Persistent volumes for SQLite
- ✅ Good pricing
- ✅ Fast deployment

**Steps:**
1. Install Fly CLI: `curl -L https://fly.io/install.sh | sh`
2. Run `fly launch` in project directory
3. Configure persistent volume for database
4. Set secrets: `fly secrets set KEY=value`
5. Deploy: `fly deploy`

**Cost:** ~$3-5/month for small apps

---

### 4. **DigitalOcean App Platform**
**Best for: Simplicity, predictable pricing**

**Pros:**
- ✅ Simple interface
- ✅ Clear pricing
- ✅ Good documentation
- ✅ Supports .NET

**Steps:**
1. Create account on DigitalOcean
2. Create App → GitHub
3. Select .NET runtime
4. Configure environment variables
5. Add persistent storage component

**Cost:** ~$5-12/month

---

### 5. **VPS (DigitalOcean Droplet, Linode, Vultr)**
**Best for: Full control, cost-effective**

**Pros:**
- ✅ Full control
- ✅ Very cost-effective ($5-10/month)
- ✅ Can run multiple apps
- ✅ Custom configuration

**Steps:**
1. Create Ubuntu VPS (2GB RAM minimum)
2. Install .NET 8 Runtime
3. Install Nginx (reverse proxy)
4. Setup systemd service
5. Configure SSL with Let's Encrypt

**Cost:** $5-10/month

---

## ⚠️ Important Notes

### SQLite in Production
Your app uses SQLite, which has limitations:
- ❌ **Not suitable for multiple instances** (if you scale horizontally)
- ✅ **OK for single instance** deployments
- ✅ **Use persistent storage** for database file

**Recommendation:**
- For single instance: SQLite is fine
- For scaling: Consider migrating to PostgreSQL or SQL Server

### Environment Variables to Set
Move sensitive data from `appsettings.json` to environment variables:

```bash
# Connection String
ConnectionStrings__DefaultConnection=Data Source=/app/data/hexmanager.db

# Email Settings
EmailSettings__SmtpServer=smtp.gmail.com
EmailSettings__SmtpPort=587
EmailSettings__SmtpUsername=your-email@gmail.com
EmailSettings__SmtpPassword=your-app-password
EmailSettings__FromEmail=your-email@gmail.com
EmailSettings__FromName=Hex Manager

# Authentication
Authentication__AuthorizedEmail=your-email@gmail.com
```

### Security Checklist
- [ ] Move all secrets to environment variables
- [ ] Enable HTTPS (most platforms do this automatically)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Review and update `AllowedHosts` in production
- [ ] Backup database regularly
- [ ] Use strong passwords for admin account

---

## 📝 Pre-Deployment Checklist

1. **Update appsettings.json for production:**
   - Remove sensitive data (move to env vars)
   - Set proper logging levels

2. **Create appsettings.Production.json:**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Warning",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "yourdomain.com"
   }
   ```

3. **Test locally:**
   ```bash
   dotnet publish -c Release
   ```

4. **Database:**
   - Ensure migrations are applied
   - Backup existing data if needed

---

## 🎯 My Recommendation

**For your use case, I recommend Railway:**
- ✅ Easiest to set up
- ✅ Good for single-user/small team apps
- ✅ Persistent storage for SQLite
- ✅ Free tier to start
- ✅ No credit card required for free tier

**Alternative: Fly.io** if you want better performance and global distribution.

---

## 📚 Additional Resources

- [Railway Documentation](https://docs.railway.app)
- [Azure App Service .NET Guide](https://docs.microsoft.com/azure/app-service/quickstart-dotnetcore)
- [Fly.io .NET Guide](https://fly.io/docs/languages-and-frameworks/dotnet/)
- [DigitalOcean App Platform](https://www.digitalocean.com/products/app-platform)
