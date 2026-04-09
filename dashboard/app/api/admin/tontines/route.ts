import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.API_URL ?? "http://localhost:8080";

export async function GET(request: NextRequest) {
  const token = request.cookies.get("accessToken")?.value;
  if (!token) {
    return NextResponse.json({ error: "Non autorisé" }, { status: 401 });
  }

  const { searchParams } = new URL(request.url);
  const page = searchParams.get("page") ?? "1";
  const pageSize = searchParams.get("pageSize") ?? "20";

  try {
    const apiRes = await fetch(
      `${API_BASE_URL}/api/v1/dashboard/admin/tontines?page=${page}&pageSize=${pageSize}`,
      {
        headers: { Authorization: `Bearer ${token}` },
        cache: "no-store",
      }
    );

    if (!apiRes.ok) {
      return NextResponse.json({ error: "Erreur API" }, { status: apiRes.status });
    }

    const data = await apiRes.json();
    return NextResponse.json(data);
  } catch {
    return NextResponse.json({ error: "Erreur serveur" }, { status: 500 });
  }
}
