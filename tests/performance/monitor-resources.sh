#!/bin/bash

set -e

echo "======================================"
echo "Resource Monitoring Script"
echo "======================================"
echo ""

DURATION=${1:-300}
INTERVAL=${2:-5}

echo "Monitoring for $DURATION seconds, sampling every $INTERVAL seconds"
echo ""

OUTPUT_DIR="/tmp/perf-monitoring"
mkdir -p "$OUTPUT_DIR"

START_TIME=$(date +%s)
COUNTER=0

echo "Timestamp,Service,CPU%,Memory_MB,Threads" > "$OUTPUT_DIR/resources.csv"

while [ $(($(date +%s) - START_TIME)) -lt $DURATION ]; do
    TIMESTAMP=$(date "+%Y-%m-%d %H:%M:%S")
    
    for SERVICE in "MarketSimulator" "MarketIngestion.Api" "Broker.Service" "redis-server"; do
        PID=$(pgrep -f "$SERVICE" | head -1)
        
        if [ -n "$PID" ]; then
            CPU=$(ps -p $PID -o %cpu= | xargs)
            MEM_KB=$(ps -p $PID -o rss= | xargs)
            MEM_MB=$(echo "scale=2; $MEM_KB/1024" | bc)
            THREADS=$(ps -p $PID -o nlwp= | xargs)
            
            echo "$TIMESTAMP,$SERVICE,$CPU,$MEM_MB,$THREADS" >> "$OUTPUT_DIR/resources.csv"
            
            if [ $COUNTER -eq 0 ]; then
                echo "[$TIMESTAMP] $SERVICE: CPU=${CPU}%, Mem=${MEM_MB}MB, Threads=$THREADS"
            fi
        fi
    done
    
    COUNTER=$((COUNTER + 1))
    if [ $COUNTER -ge 12 ]; then
        COUNTER=0
    fi
    
    sleep $INTERVAL
done

echo ""
echo "Monitoring complete. Results saved to: $OUTPUT_DIR/resources.csv"
echo ""
echo "Summary:"
echo "--------"

for SERVICE in "MarketSimulator" "MarketIngestion.Api" "Broker.Service" "redis-server"; do
    echo ""
    echo "$SERVICE:"
    awk -F, -v svc="$SERVICE" '$2 == svc {
        cpu+=$3; mem+=$4; count++
        if(NR==1 || $3>max_cpu) max_cpu=$3
        if(NR==1 || $4>max_mem) max_mem=$4
    }
    END {
        if(count>0) {
            printf "  Avg CPU: %.2f%%\n", cpu/count
            printf "  Max CPU: %.2f%%\n", max_cpu
            printf "  Avg Mem: %.2fMB\n", mem/count
            printf "  Max Mem: %.2fMB\n", max_mem
        }
    }' "$OUTPUT_DIR/resources.csv"
done

echo ""
