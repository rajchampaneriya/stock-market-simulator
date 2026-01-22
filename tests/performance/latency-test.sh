#!/bin/bash

set -e

echo "======================================"
echo "End-to-End Latency Test"
echo "======================================"
echo ""

ITERATIONS=${1:-100}
URL=${2:-http://localhost:5001/api/marketdata}

echo "Running $ITERATIONS requests to $URL"
echo ""

OUTPUT_FILE="/tmp/latency-results.txt"
> "$OUTPUT_FILE"

for i in $(seq 1 $ITERATIONS); do
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S.%3NZ")
    PAYLOAD="{\"symbol\":\"TEST\",\"price\":$(shuf -i 100-1000 -n 1),\"volume\":$(shuf -i 1000-10000 -n 1),\"timestamp\":\"$TIMESTAMP\"}"
    
    TIME=$(curl -w "%{time_total}\n" -o /dev/null -s \
        -X POST "$URL" \
        -H "Content-Type: application/json" \
        -d "$PAYLOAD")
    
    echo "$TIME" >> "$OUTPUT_FILE"
    
    if [ $((i % 10)) -eq 0 ]; then
        echo -ne "\rProgress: $i/$ITERATIONS requests"
    fi
done

echo ""
echo ""
echo "Results:"
echo "--------"

sort -n "$OUTPUT_FILE" | awk '
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
    
    printf "Total Requests: %d\n", count
    printf "Average Latency: %.3f ms\n", (sum/count)*1000
    printf "Min Latency: %.3f ms\n", times[0]*1000
    printf "Max Latency: %.3f ms\n", times[count-1]*1000
    printf "p50 Latency: %.3f ms\n", times[p50_idx]*1000
    printf "p95 Latency: %.3f ms\n", times[p95_idx]*1000
    printf "p99 Latency: %.3f ms\n", times[p99_idx]*1000
}
'

echo ""
echo "Detailed results saved to: $OUTPUT_FILE"
echo ""
