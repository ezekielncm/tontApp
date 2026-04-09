import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.API_URL ?? "http://localhost:8080";

export async function POST(request: NextRequest) {
  try {
    const token = request.cookies.get("accessToken")?.value;
    if (!token) {
      return NextResponse.json({ error: "Non autorisé" }, { status: 401 });
    }

    const body = await request.json();

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
