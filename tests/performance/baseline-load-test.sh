#!/bin/bash

set -e

echo "======================================"
echo "Baseline Load Test"
echo "Using curl for load testing"
echo "======================================"
echo ""

DURATION=${1:-120}
INTERVAL=${2:-0.3}
URL=${3:-http://localhost:5001/api/marketdata}

SYMBOLS=("NIFTY50" "BANKNIFTY" "RELIANCE" "TCS")

echo "Duration: ${DURATION}s"
echo "Interval: ${INTERVAL}s between requests"
echo "Target: $URL"
echo ""

OUTPUT_FILE="/tmp/baseline-load-test.txt"
> "$OUTPUT_FILE"

START_TIME=$(date +%s)
REQUEST_COUNT=0
SUCCESS_COUNT=0
TOTAL_TIME=0

while [ $(($(date +%s) - START_TIME)) -lt $DURATION ]; do
    SYMBOL=${SYMBOLS[$RANDOM % ${#SYMBOLS[@]}]}
    PRICE=$(echo "scale=2; $(shuf -i 100-1000 -n 1) + $(shuf -i 0-99 -n 1)/100" | bc)
    VOLUME=$(shuf -i 1000-100000 -n 1)
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")
    
    PAYLOAD="{\"symbol\":\"$SYMBOL\",\"price\":$PRICE,\"volume\":$VOLUME,\"timestamp\":\"$TIMESTAMP\"}"
    
    RESPONSE=$(curl -w "\n%{http_code}|%{time_total}" -o /dev/null -s \
        -X POST "$URL" \
        -H "Content-Type: application/json" \
        -d "$PAYLOAD")
    
    HTTP_CODE=$(echo "$RESPONSE" | tail -1 | cut -d'|' -f1)
    TIME=$(echo "$RESPONSE" | tail -1 | cut -d'|' -f2)
    
    echo "$TIME" >> "$OUTPUT_FILE"
    
    REQUEST_COUNT=$((REQUEST_COUNT + 1))
    if [ "$HTTP_CODE" == "200" ]; then
        SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
        TOTAL_TIME=$(echo "$TOTAL_TIME + $TIME" | bc)
    fi
    
    if [ $((REQUEST_COUNT % 10)) -eq 0 ]; then
        ELAPSED=$(($(date +%s) - START_TIME))
        echo -ne "\rProgress: ${ELAPSED}s / ${DURATION}s | Requests: $REQUEST_COUNT | Success: $SUCCESS_COUNT"
    fi
    
    sleep $INTERVAL
done

echo ""
echo ""
echo "======================================"
echo "Baseline Load Test Results"
echo "======================================"
echo ""

sort -n "$OUTPUT_FILE" | awk -v total=$REQUEST_COUNT -v success=$SUCCESS_COUNT -v total_time=$TOTAL_TIME '
BEGIN {
    count = 0
    sum = 0
}
{
    times[count] = $1
    sum += $1
    count++
}
END {
    p50_idx = int(count * 0.50)
    p95_idx = int(count * 0.95)
    p99_idx = int(count * 0.99)
    
    printf "Total Requests: %d\n", total
    printf "Successful Requests: %d (%.2f%%)\n", success, (success/total)*100
    printf "Failed Requests: %d\n", total-success
    printf "\n"
    printf "Latency Statistics:\n"
    printf "-------------------\n"
    printf "Min: %.3f ms\n", times[0]*1000
    printf "Max: %.3f ms\n", times[count-1]*1000
    printf "Average: %.3f ms\n", (sum/count)*1000
    printf "p50: %.3f ms\n", times[p50_idx]*1000
    printf "p95: %.3f ms\n", times[p95_idx]*1000
    printf "p99: %.3f ms\n", times[p99_idx]*1000
    printf "\n"
    
    if(times[p95_idx]*1000 < 300) {
        printf "✅ p95 latency meets target (< 300ms)\n"
    } else if(times[p95_idx]*1000 < 500) {
        printf "⚠️  p95 latency acceptable (< 500ms)\n"
    } else {
        printf "❌ p95 latency poor (> 500ms)\n"
    }
}
'

echo ""
echo "Detailed results saved to: $OUTPUT_FILE"
echo ""
