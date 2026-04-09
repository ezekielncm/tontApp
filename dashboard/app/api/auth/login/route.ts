import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.API_URL ?? "http://localhost:8080";

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    const apiRes = await fetch(`${API_BASE_URL}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });

    if (!apiRes.ok) {
      const error = await apiRes.json().catch(() => ({ error: "Identifiants invalides" }));
      return NextResponse.json(error, { status: apiRes.status });
    }

    const data = await apiRes.json();

    // Set HTTP-only secure cookie with the JWT
    const response = NextResponse.json({ success: true });
    response.cookies.set("accessToken", data.accessToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: 60 * 60 * 24, // 24 hours
    });

    response.cookies.set("refreshToken", data.refreshToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/api/auth",
      maxAge: 60 * 60 * 24 * 7, // 7 days
    });

    return response;
  } catch {
    return NextResponse.json(
      { error: "Erreur de connexion au serveur" },
      { status: 502 }
    );
  }
}
