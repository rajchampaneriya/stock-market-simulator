#!/bin/bash

set -e

echo "======================================"
echo "Redis Statistics Monitor"
echo "======================================"
echo ""

if ! command -v redis-cli &> /dev/null; then
    echo "Error: redis-cli not found. Please install redis-tools."
    exit 1
fi

REDIS_HOST=${REDIS_HOST:-localhost}
REDIS_PORT=${REDIS_PORT:-6379}

echo "Connecting to Redis at $REDIS_HOST:$REDIS_PORT"
echo ""

echo "Stream Information:"
echo "-------------------"
redis-cli -h $REDIS_HOST -p $REDIS_PORT XINFO STREAM marketdata:stream || echo "Stream does not exist yet"
echo ""

echo "Consumer Groups:"
echo "----------------"
redis-cli -h $REDIS_HOST -p $REDIS_PORT XINFO GROUPS marketdata:stream 2>/dev/null || echo "No consumer groups"
echo ""

echo "Stream Length:"
echo "--------------"
STREAM_LEN=$(redis-cli -h $REDIS_HOST -p $REDIS_PORT XLEN marketdata:stream)
echo "Messages in stream: $STREAM_LEN"
echo ""

echo "Redis Memory Stats:"
echo "-------------------"
redis-cli -h $REDIS_HOST -p $REDIS_PORT INFO memory | grep -E "used_memory_human|used_memory_peak_human|maxmemory_human"
echo ""

echo "Redis Stats:"
echo "------------"
redis-cli -h $REDIS_HOST -p $REDIS_PORT INFO stats | grep -E "total_commands_processed|instantaneous_ops_per_sec"
echo ""
