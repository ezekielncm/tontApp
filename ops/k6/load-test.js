/**
 * TontinesApp — k6 Load Test Script
 * ==================================
 * Tests critical endpoints under realistic load:
 *   - POST /api/v1/auth/login            → 50 VU
 *   - POST /api/v1/versements (create)   → 20 VU
 *   - POST /api/v1/webhooks/orange-money → 10 VU
 *
 * Duration: 5 minutes per scenario
 * Assertions: p(95) < 500ms, error rate < 1%
 *
 * Usage:
 *   k6 run --out json=results.json ops/k6/load-test.js
 *   k6 run --out json=results.json --env BASE_URL=https://staging.tontinesapp.com ops/k6/load-test.js
 *
 * HTML Report (requires k6-reporter extension):
 *   k6 run --out json=results.json ops/k6/load-test.js
 *   # Then generate: k6-reporter results.json -o report.html
 *
 * ⚠️  NEVER target production Orange Money — use Africa's Talking sandbox only.
 */

import http from "k6/http";
import { check, group, sleep, fail } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";
import { SharedArray } from "k6/data";
import { randomString, randomIntBetween } from "https://jslib.k6.io/k6-utils/1.4.0/index.js";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.1.0/index.js";

// ─── Configuration ──────────────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const API_V1 = `${BASE_URL}/api/v1`;

// Webhook HMAC secret for signature generation (sandbox only)
const WEBHOOK_HMAC_SECRET = __ENV.WEBHOOK_HMAC_SECRET || "test-webhook-secret";

// ─── Custom Metrics ─────────────────────────────────────────────────────────
const loginDuration = new Trend("login_duration", true);
const versementDuration = new Trend("versement_create_duration", true);
const webhookDuration = new Trend("webhook_duration", true);
const loginErrors = new Rate("login_errors");
const versementErrors = new Rate("versement_errors");
const webhookErrors = new Rate("webhook_errors");
const successfulLogins = new Counter("successful_logins");
const successfulVersements = new Counter("successful_versements");
const successfulWebhooks = new Counter("successful_webhooks");

// ─── Test Users (pre-seeded in staging DB) ──────────────────────────────────
const TEST_USERS = new SharedArray("users", function () {
  const users = [];
  for (let i = 1; i <= 50; i++) {
    users.push({
      telephone: `+2257000${String(i).padStart(4, "0")}`,
      motDePasse: "TestPassword123!",
      nom: `LoadTestUser${i}`,
    });
  }
  return users;
});

// ─── Test Tontine Data ──────────────────────────────────────────────────────
const TEST_TONTINE_IDS = new SharedArray("tontines", function () {
  // These should be pre-seeded tontine IDs in the staging environment
  // Replace with actual UUIDs from your staging database
  const ids = [];
  for (let i = 0; i < 20; i++) {
    ids.push({
      tontineId: `00000000-0000-0000-0000-${String(i + 1).padStart(12, "0")}`,
      tourId: `10000000-0000-0000-0000-${String(i + 1).padStart(12, "0")}`,
      payeurId: `20000000-0000-0000-0000-${String(i + 1).padStart(12, "0")}`,
    });
  }
  return ids;
});

// ─── Scenarios ──────────────────────────────────────────────────────────────
export const options = {
  scenarios: {
    // Scenario 1: Login — 50 Virtual Users, 5 minutes
    login_load: {
      executor: "constant-vus",
      vus: 50,
      duration: "5m",
      exec: "loginScenario",
      tags: { scenario: "login" },
      gracefulStop: "30s",
    },

    // Scenario 2: Create Versement — 20 Virtual Users, 5 minutes
    versement_load: {
      executor: "constant-vus",
      vus: 20,
      duration: "5m",
      exec: "versementScenario",
      tags: { scenario: "versement" },
      startTime: "0s", // Run concurrently with login
      gracefulStop: "30s",
    },

    // Scenario 3: Webhook Orange Money — 10 Virtual Users, 5 minutes
    // ⚠️ Uses Africa's Talking SANDBOX only — never production
    webhook_load: {
      executor: "constant-vus",
      vus: 10,
      duration: "5m",
      exec: "webhookScenario",
      tags: { scenario: "webhook" },
      startTime: "0s", // Run concurrently
      gracefulStop: "30s",
    },
  },

  // ─── Thresholds (SLO) ──────────────────────────────────────────────────
  thresholds: {
    // Global
    http_req_failed: ["rate<0.01"], // Global error rate < 1%

    // Login: p95 < 500ms
    login_duration: ["p(95)<500"],
    login_errors: ["rate<0.01"],

    // Versement creation: p95 < 500ms
    versement_create_duration: ["p(95)<500"],
    versement_errors: ["rate<0.01"],

    // Webhook: p95 < 500ms
    webhook_duration: ["p(95)<500"],
    webhook_errors: ["rate<0.01"],
  },
};

// ─── Setup: Register test users and create test data ────────────────────────
export function setup() {
  console.log(`🎯 Target: ${BASE_URL}`);
  console.log("📋 Scenarios: login (50 VU), versement (20 VU), webhook (10 VU)");
  console.log("⏱️  Duration: 5 minutes per scenario");
  console.log("🎯 SLO: p95 < 500ms, error rate < 1%");
  console.log("");

  // Health check
  const healthRes = http.get(`${BASE_URL}/health`);
  if (healthRes.status !== 200) {
    fail(`API health check failed: ${healthRes.status}`);
  }

  // Register test users (idempotent — 409 Conflict means already exists)
  const tokens = {};
  for (let i = 0; i < TEST_USERS.length; i++) {
    const user = TEST_USERS[i];
    const registerRes = http.post(
      `${API_V1}/auth/register`,
      JSON.stringify({
        telephone: user.telephone,
        nom: user.nom,
        motDePasse: user.motDePasse,
      }),
      { headers: { "Content-Type": "application/json" } }
    );

    if (registerRes.status === 201 || registerRes.status === 409) {
      // Login to get token
      const loginRes = http.post(
        `${API_V1}/auth/login`,
        JSON.stringify({
          telephone: user.telephone,
          motDePasse: user.motDePasse,
        }),
        { headers: { "Content-Type": "application/json" } }
      );

      if (loginRes.status === 200) {
        const body = JSON.parse(loginRes.body);
        tokens[i] = body.accessToken;
      }
    }
  }

  console.log(`✅ Setup complete: ${Object.keys(tokens).length} users authenticated`);
  return { tokens };
}

// ═══════════════════════════════════════════════════════════════════════════
// SCENARIO 1: Login (50 VU)
// ═══════════════════════════════════════════════════════════════════════════
export function loginScenario() {
  const userIndex = (__VU - 1) % TEST_USERS.length;
  const user = TEST_USERS[userIndex];

  group("POST /api/v1/auth/login", function () {
    const payload = JSON.stringify({
      telephone: user.telephone,
      motDePasse: user.motDePasse,
    });

    const params = {
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      tags: { name: "login" },
    };

    const res = http.post(`${API_V1}/auth/login`, payload, params);

    // Record custom metrics
    loginDuration.add(res.timings.duration);

    const success = check(res, {
      "login: status is 200": (r) => r.status === 200,
      "login: response has accessToken": (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.accessToken !== undefined;
        } catch {
          return false;
        }
      },
      "login: response time < 500ms": (r) => r.timings.duration < 500,
      "login: response has refreshToken": (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.refreshToken !== undefined;
        } catch {
          return false;
        }
      },
    });

    loginErrors.add(!success);
    if (success) successfulLogins.add(1);
  });

  // Think time: simulate user behavior (1-3 seconds between requests)
  sleep(randomIntBetween(1, 3));
}

// ═══════════════════════════════════════════════════════════════════════════
// SCENARIO 2: Create Versement (20 VU)
// ═══════════════════════════════════════════════════════════════════════════
export function versementScenario(data) {
  const userIndex = (__VU - 1) % TEST_USERS.length;
  const user = TEST_USERS[userIndex];
  const tontineData = TEST_TONTINE_IDS[__VU % TEST_TONTINE_IDS.length];

  group("Create Versement Flow", function () {
    // Step 1: Authenticate
    const loginRes = http.post(
      `${API_V1}/auth/login`,
      JSON.stringify({
        telephone: user.telephone,
        motDePasse: user.motDePasse,
      }),
      {
        headers: { "Content-Type": "application/json" },
        tags: { name: "versement_auth" },
      }
    );

    if (loginRes.status !== 200) {
      versementErrors.add(true);
      return;
    }

    let accessToken;
    try {
      accessToken = JSON.parse(loginRes.body).accessToken;
    } catch {
      versementErrors.add(true);
      return;
    }

    // Step 2: Create Versement (Initiate Orange Money Payment)
    const versementPayload = JSON.stringify({
      tontineId: tontineData.tontineId,
      tourId: tontineData.tourId,
      montant: randomIntBetween(1, 50) * 100, // 100-5000 FCFA inclusive (multiples of 100)
      devise: "XOF",
      numeroTelephone: user.telephone,
    });

    const versementRes = http.post(
      `${API_V1}/versements`,
      versementPayload,
      {
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        tags: { name: "create_versement" },
      }
    );

    // Record custom metrics
    versementDuration.add(versementRes.timings.duration);

    const success = check(versementRes, {
      "versement: status is 201 or 200": (r) =>
        r.status === 201 || r.status === 200,
      "versement: response time < 500ms": (r) => r.timings.duration < 500,
      "versement: has versement ID": (r) => {
        try {
          const body = JSON.parse(r.body);
          return body.id !== undefined || body.versementId !== undefined;
        } catch {
          return false;
        }
      },
    });

    versementErrors.add(!success);
    if (success) successfulVersements.add(1);
  });

  // Think time
  sleep(randomIntBetween(2, 5));
}

// ═══════════════════════════════════════════════════════════════════════════
// SCENARIO 3: Webhook Orange Money (10 VU)
// ═══════════════════════════════════════════════════════════════════════════
export function webhookScenario() {
  group("POST /api/v1/webhooks/orange-money", function () {
    // Simulate Africa's Talking sandbox webhook callback
    const versementId = `30000000-0000-0000-0000-${String(
      randomIntBetween(1, 999999)
    ).padStart(12, "0")}`;

    const transactionId = `AT_SANDBOX_${randomString(16)}`;

    const webhookPayload = JSON.stringify({
      transactionId: transactionId,
      status: "Success",
      description: "Payment completed successfully",
      category: "MobileB2C",
      provider: "Mpesa",
      providerChannel: "525900",
      value: `XOF ${randomIntBetween(1, 50) * 100}`,
      requestMetadata: {
        reference: versementId,
      },
    });

    // Compute HMAC-SHA256 signature (simulating Africa's Talking)
    // Note: In k6 we simulate the signature; the API validates it
    const params = {
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        "X-AfricasTalking-Signature": computeHmacPlaceholder(webhookPayload),
      },
      tags: { name: "webhook_orange_money" },
    };

    const res = http.post(
      `${API_V1}/webhooks/orange-money`,
      webhookPayload,
      params
    );

    // Record custom metrics
    webhookDuration.add(res.timings.duration);

    // Accept 200 (processed), 400 (versement not found — expected in load test),
    // or 401 (HMAC mismatch — expected if secret doesn't match)
    const success = check(res, {
      "webhook: status is 200 or 400": (r) =>
        r.status === 200 || r.status === 400,
      "webhook: response time < 500ms": (r) => r.timings.duration < 500,
      "webhook: response has body": (r) => r.body && r.body.length > 0,
    });

    webhookErrors.add(!success);
    if (success) successfulWebhooks.add(1);
  });

  // Think time
  sleep(randomIntBetween(1, 2));
}

// ─── HMAC Helper ────────────────────────────────────────────────────────────
// k6 doesn't have native crypto.createHmac — use a placeholder for signature.
// For proper HMAC testing, configure the staging API with a known test secret
// and use the k6 crypto extension: https://github.com/nicholasgasior/xk6-hmac
function computeHmacPlaceholder(payload) {
  // In production load tests, use xk6-hmac extension:
  //   import { hmac } from 'k6/x/hmac';
  //   return hmac('sha256', WEBHOOK_HMAC_SECRET, payload, 'hex');
  //
  // For sandbox testing, the API should accept this test signature
  // or be configured with WEBHOOK_HMAC_SECRET matching this value.
  return __ENV.WEBHOOK_TEST_SIGNATURE || "test-signature-for-load-testing";
}

// ─── HTML Report & Summary ──────────────────────────────────────────────────
export function handleSummary(data) {
  const timestamp = new Date().toISOString().replace(/[:.]/g, "-");

  return {
    [`ops/k6/reports/summary-${timestamp}.html`]: htmlReport(data),
    [`ops/k6/reports/summary-${timestamp}.json`]: JSON.stringify(data, null, 2),
    stdout: textSummary(data, { indent: "  ", enableColors: true }),
  };
}
