import { redirect } from "next/navigation";

export default function Home() {
  // Default redirect to gestionnaire dashboard.
  // Middleware will handle auth check and role-based redirection.
  redirect("/gestionnaire");
}

