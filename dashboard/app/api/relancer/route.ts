import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.API_URL ?? "http://localhost:8080";

// UUID v4 regex for input validation to prevent SSRF
const UUID_REGEX =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export async function POST(request: NextRequest) {
  try {
    const token = request.cookies.get("accessToken")?.value;
    if (!token) {
      return NextResponse.json({ error: "Non autorisé" }, { status: 401 });
    }

    const body = await request.json();

    // Validate IDs are proper UUIDs to prevent path traversal / SSRF
    if (
      typeof body.tontineId !== "string" ||
      !UUID_REGEX.test(body.tontineId) ||
      typeof body.membreId !== "string" ||
      !UUID_REGEX.test(body.membreId)
    ) {
      return NextResponse.json(
        { error: "Identifiants invalides" },
        { status: 400 }
      );
    }

    // Forward the relancer request to the ASP.NET API
    const apiRes = await fetch(
      `${API_BASE_URL}/api/v1/tontines/${body.tontineId}/members/${body.membreId}/relancer`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      }
    );

    if (!apiRes.ok) {
      const error = await apiRes.text().catch(() => "Erreur lors de la relance");
      return NextResponse.json({ error }, { status: apiRes.status });
    }

    return NextResponse.json({ success: true });
  } catch {
    return NextResponse.json(
      { error: "Erreur serveur" },
      { status: 500 }
    );
  }
}
