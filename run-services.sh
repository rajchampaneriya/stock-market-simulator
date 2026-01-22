#!/bin/bash

# Stock Market Simulator - Service Runner
# Starts all services without Aspire orchestration
# Workaround for .NET 10 Aspire DCP unavailability

set -e

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configure .NET 10
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"

echo -e "${GREEN}Stock Market Simulator - Service Launcher${NC}"
echo "=========================================="
echo ""

# Check prerequisites
echo "Checking prerequisites..."

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
echo -e "✓ .NET SDK: ${GREEN}$DOTNET_VERSION${NC}"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}✗ Docker not found${NC}"
    echo "Please install Docker Desktop"
    exit 1
fi
echo -e "✓ Docker: ${GREEN}$(docker --version)${NC}"

# Check if Docker daemon is running
if ! docker ps &> /dev/null; then
    echo -e "${YELLOW}⚠ Docker daemon not running${NC}"
    echo "Please start Docker Desktop"
    exit 1
fi
echo -e "✓ Docker daemon: ${GREEN}Running${NC}"

echo ""
echo "Starting services..."
echo ""

# Start Redis
echo "1. Starting Redis..."
if docker ps -a | grep -q stock-redis; then
    if docker ps | grep -q stock-redis; then
        echo -e "   ${GREEN}✓ Redis already running${NC}"
    else
        docker start stock-redis
        echo -e "   ${GREEN}✓ Redis started${NC}"
    fi
else
    docker run -d --name stock-redis -p 6379:6379 redis:latest
    echo -e "   ${GREEN}✓ Redis container created and started${NC}"
fi

# Wait for Redis to be ready
sleep 2

# Build all projects
echo ""
echo "2. Building projects..."
cd "$(dirname "$0")/src"
dotnet build --verbosity quiet
echo -e "   ${GREEN}✓ Build successful${NC}"

echo ""
echo "=========================================="
echo -e "${GREEN}All infrastructure ready!${NC}"
echo ""
echo "To start the applications, run these commands in separate terminals:"
echo ""
echo -e "${YELLOW}Terminal 1 - Market Ingestion API:${NC}"
echo "  cd src/MarketIngestion.Api"
echo "  export PATH=\"\$HOME/.dotnet:\$PATH\""
echo "  dotnet run --urls \"http://localhost:5001\""
echo ""
echo -e "${YELLOW}Terminal 2 - Broker Service:${NC}"
echo "  cd src/Broker.Service"
echo "  export PATH=\"\$HOME/.dotnet:\$PATH\""
echo "  dotnet run --urls \"http://localhost:5002\""
echo ""
echo -e "${YELLOW}Terminal 3 - Market Simulator:${NC}"
echo "  cd src/MarketSimulator"
echo "  export PATH=\"\$HOME/.dotnet:\$PATH\""
echo "  export IngestionApiUrl=\"http://localhost:5001\""
echo "  dotnet run"
echo ""
echo "=========================================="
echo ""
echo "To stop all services:"
echo "  docker stop stock-redis"
echo ""
echo "To remove Redis container:"
echo "  docker stop stock-redis && docker rm stock-redis"
echo ""
