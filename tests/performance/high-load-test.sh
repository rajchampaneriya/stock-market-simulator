#!/bin/bash

set -e

echo "======================================"
echo "High Load Test"
echo "Using concurrent curl for load testing"
echo "======================================"
echo ""

DURATION=${1:-300}
CONCURRENCY=${2:-20}
RATE_PER_SECOND=${3:-10}
URL=${4:-http://localhost:5001/api/marketdata}

SYMBOLS=("NIFTY50" "BANKNIFTY" "RELIANCE" "TCS" "INFY" "HDFC" "ICICI" "SBIN" "BHARTIARTL" "ITC" "KOTAKBANK" "LT" "HCLTECH" "AXISBANK" "WIPRO" "MARUTI" "SUNPHARMA" "TATASTEEL" "ULTRACEMCO" "TITAN")

echo "Duration: ${DURATION}s"
echo "Concurrency: ${CONCURRENCY} parallel requests"
echo "Target rate: ${RATE_PER_SECOND} req/s"
echo "Target: $URL"
echo ""

OUTPUT_FILE="/tmp/high-load-test.txt"
> "$OUTPUT_FILE"

INTERVAL=$(echo "scale=3; 1.0/$RATE_PER_SECOND" | bc)

make_request() {
    local url=$1
    local output=$2
    
    SYMBOL=${SYMBOLS[$RANDOM % ${#SYMBOLS[@]}]}
    PRICE=$(echo "scale=2; $(shuf -i 50-2000 -n 1) + $(shuf -i 0-99 -n 1)/100" | bc)
    VOLUME=$(shuf -i 10000-200000 -n 1)
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")
    
    PAYLOAD="{\"symbol\":\"$SYMBOL\",\"price\":$PRICE,\"volume\":$VOLUME,\"timestamp\":\"$TIMESTAMP\"}"
    
    RESPONSE=$(curl -w "\n%{http_code}|%{time_total}" -o /dev/null -s \
        -X POST "$url" \
        -H "Content-Type: application/json" \
        -d "$PAYLOAD")
    
    HTTP_CODE=$(echo "$RESPONSE" | tail -1 | cut -d'|' -f1)
    TIME=$(echo "$RESPONSE" | tail -1 | cut -d'|' -f2)
    
    echo "$HTTP_CODE|$TIME" >> "$output"
}

export -f make_request
export SYMBOLS
export URL
export OUTPUT_FILE

START_TIME=$(date +%s)
REQUEST_COUNT=0

echo "Starting high load test..."
echo ""

while [ $(($(date +%s) - START_TIME)) -lt $DURATION ]; do
    for i in $(seq 1 $CONCURRENCY); do
        make_request "$URL" "$OUTPUT_FILE" &
    done
    
    REQUEST_COUNT=$((REQUEST_COUNT + CONCURRENCY))
    
    if [ $((REQUEST_COUNT % 100)) -eq 0 ]; then
        ELAPSED=$(($(date +%s) - START_TIME))
        echo -ne "\rProgress: ${ELAPSED}s / ${DURATION}s | Requests: $REQUEST_COUNT"
    fi
    
    sleep $INTERVAL
    wait
done

wait

echo ""
echo ""
echo "======================================"
echo "High Load Test Results"
echo "======================================"
echo ""

awk -F'|' '
BEGIN {
    total = 0
    success = 0
    count = 0
    sum = 0
}
{
    total++
    if($1 == "200") {
        success++
        times[count] = $2
        sum += $2
        count++
    }
}
END {
    if(count > 0) {
        asort(times)
        p50_idx = int(count * 0.50)
        p95_idx = int(count * 0.95)
        p99_idx = int(count * 0.99)
        
        printf "Total Requests: %d\n", total
        printf "Successful Requests: %d (%.2f%%)\n", success, (success/total)*100
        printf "Failed Requests: %d\n", total-success
        printf "\n"
        printf "Latency Statistics:\n"
        printf "-------------------\n"
        printf "Min: %.3f ms\n", times[1]*1000
        printf "Max: %.3f ms\n", times[count]*1000
        printf "Average: %.3f ms\n", (sum/count)*1000
        printf "p50: %.3f ms\n", times[p50_idx]*1000
        printf "p95: %.3f ms\n", times[p95_idx]*1000
        printf "p99: %.3f ms\n", times[p99_idx]*1000
        printf "\n"
        
        if(times[p95_idx]*1000 < 500) {
            printf "✅ p95 latency meets target (< 500ms)\n"
        } else {
            printf "❌ p95 latency poor (> 500ms)\n"
        }
        
        if((success/total) > 0.95) {
            printf "✅ Success rate acceptable (> 95%%)\n"
        } else {
            printf "❌ Success rate poor (< 95%%)\n"
        }
    } else {
        printf "No successful requests!\n"
    }
}
' "$OUTPUT_FILE"

echo ""
echo "Detailed results saved to: $OUTPUT_FILE"
echo ""
