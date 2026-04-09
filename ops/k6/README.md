# TontinesApp — k6 Load Tests

## Overview

Load tests for the three critical API endpoints:

| Scenario          | Endpoint                           | VU  | Duration | SLO (p95) |
| ----------------- | ---------------------------------- | --- | -------- | --------- |
| Login             | `POST /api/v1/auth/login`          | 50  | 5 min    | < 500ms   |
| Create Versement  | `POST /api/v1/versements`          | 20  | 5 min    | < 500ms   |
| Webhook Orange Money | `POST /api/v1/webhooks/orange-money` | 10 | 5 min | < 500ms |

## Prerequisites

1. Install k6: https://k6.io/docs/getting-started/installation/
2. Staging environment must be running with seeded test data
3. **⚠️ NEVER target production Orange Money — use Africa's Talking sandbox only**

## Running

```bash
# Against local environment
k6 run ops/k6/load-test.js

# Against staging
k6 run --env BASE_URL=https://staging.tontinesapp.com ops/k6/load-test.js

# With custom webhook secret
k6 run --env WEBHOOK_HMAC_SECRET=your-staging-secret ops/k6/load-test.js
```

## Reports

HTML reports are generated in `ops/k6/reports/` after each run.

## Staging Data Setup

Before running, seed the staging database with:
- 50 test users (`+2257000XXXX` series)
- Pre-created tontines with open tours
- Configure `WEBHOOK_HMAC_SECRET` to match the staging API
