#!/bin/bash
set -e

echo "=== MyLxCar Deploy Script (Oracle Cloud Free Tier) ==="

DOMAIN="mylxcar.online"
GIT_REPO="https://github.com/Ngtnua206/CarProject.git"
APP_DIR="/opt/mylxcar"
PROJECT_DIR="$APP_DIR/CarProject/CarProject"

# Step 1: Install dependencies
echo "[1/7] Installing Docker, Nginx, .NET SDK..."
sudo apt update && sudo apt upgrade -y
sudo apt install -y docker.io docker-compose-v2 nginx certbot python3-certbot-nginx git

# Install .NET SDK 10.0
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
sudo /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
sudo ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet

sudo systemctl enable --now docker

# Step 2: Clone code
echo "[2/7] Cloning code from GitHub..."
sudo mkdir -p $APP_DIR
sudo git clone $GIT_REPO $APP_DIR

# Step 3: Start SQL Server
echo "[3/7] Starting SQL Server container..."
cd $APP_DIR
sudo docker compose -f deploy/docker-compose.yml up -d

# Wait for SQL Server
echo "Waiting for SQL Server to be ready..."
sleep 30
sudo docker exec carshop-db /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "Iumaioanhh@2024" -C \
    -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='CarShopDb') CREATE DATABASE CarShopDb" || true

# Step 4: Publish .NET app
echo "[4/7] Building and publishing .NET app..."
cd $PROJECT_DIR
sudo dotnet publish -c Release -o /opt/mylxcar-app --self-contained false

# Step 5: Create systemd service
echo "[5/7] Creating systemd service..."
sudo tee /etc/systemd/system/mylxcar.service > /dev/null <<'SERVICE'
[Unit]
Description=MyLxCar Web App
After=network.target docker.service
Requires=docker.service

[Service]
WorkingDirectory=/opt/mylxcar-app
ExecStart=/usr/share/dotnet/dotnet /opt/mylxcar-app/CarProject.dll --urls "http://0.0.0.0:5001"
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mylxcar
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5001
Environment=ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=CarShopDb;User Id=sa;Password=Iumaioanhh@2024;TrustServerCertificate=True

[Install]
WantedBy=multi-user.target
SERVICE

sudo systemctl daemon-reload
sudo systemctl enable --now mylxcar

# Step 6: Configure Nginx reverse proxy
echo "[6/7] Configuring Nginx..."
sudo tee /etc/nginx/sites-available/mylxcar > /dev/null <<'NGINX'
server {
    listen 80;
    server_name mylxcar.online www.mylxcar.online;

    location / {
        proxy_pass http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
NGINX

sudo ln -sf /etc/nginx/sites-available/mylxcar /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx

# Step 7: SSL certificate
echo "[7/7] Obtaining SSL certificate..."
echo ""
echo "============================================"
echo "IMPORTANT: Before continuing, make sure your"
echo "domain $DOMAIN points to this server's IP!"
echo ""
echo "1. Go to your domain registrar's DNS settings"
echo "2. Add an A record: @ -> (this server's IP)"
echo "3. Add an A record: www -> (this server's IP)"
echo "============================================"
echo ""
read -p "Done with DNS? Press Enter to continue..."

sudo certbot --nginx -d $DOMAIN -d www.$DOMAIN --non-interactive --agree-tos \
    -m admin@$DOMAIN || echo "SSL failed. Run later: sudo certbot --nginx -d $DOMAIN"

echo ""
echo "=== DEPLOYMENT COMPLETE ==="
echo "Website: https://$DOMAIN"
echo ""
echo "Useful commands:"
echo "  View app logs: sudo journalctl -u mylxcar -f"
echo "  Restart app:   sudo systemctl restart mylxcar"
echo "  SQL Server:    sudo docker logs carshop-db -f"
echo "  Update code:"
echo "    cd $APP_DIR && sudo git pull"
echo "    cd $PROJECT_DIR && sudo dotnet publish -c Release -o /opt/mylxcar-app --self-contained false"
echo "    sudo systemctl restart mylxcar"
