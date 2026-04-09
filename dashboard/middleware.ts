import { NextRequest, NextResponse } from "next/server";
import { jwtVerify, importSPKI, importX509 } from "jose";

const PUBLIC_PATHS = ["/login", "/api/auth", "/_next", "/favicon.ico"];

function isPublicPath(pathname: string): boolean {
  return PUBLIC_PATHS.some((p) => pathname.startsWith(p));
}

export async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Allow public routes
  if (isPublicPath(pathname)) {
    return NextResponse.next();
  }

  const token =
    request.cookies.get("accessToken")?.value ??
    request.headers.get("authorization")?.replace("Bearer ", "");

  if (!token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  try {
    const secret = new TextEncoder().encode(
      process.env.JWT_SECRET ?? "CHANGE_ME_min_32_chars_secure_random_key"
    );

    const { payload } = await jwtVerify(token, secret, {
      issuer: process.env.JWT_ISSUER ?? "TontinesApp",
      audience: process.env.JWT_AUDIENCE ?? "TontinesApp",
    });

    const role = (payload as Record<string, unknown>)[
      "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    ] as string | undefined;

    // Role-based access control
    if (pathname.startsWith("/admin") && role !== "Admin") {
      return NextResponse.json({ error: "Forbidden" }, { status: 403 });
    }

    if (
      pathname.startsWith("/gestionnaire") &&
      role !== "Gestionnaire" &&
      role !== "Admin"
    ) {
      return NextResponse.json({ error: "Forbidden" }, { status: 403 });
    }

    // Forward user info via headers for SSR components
    const response = NextResponse.next();
    response.headers.set("x-user-id", (payload.sub as string) ?? "");
    response.headers.set("x-user-role", role ?? "");
    response.headers.set(
      "x-user-nom",
      (payload as Record<string, unknown>)["nom"] as string ?? ""
    );
    return response;
  } catch {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
