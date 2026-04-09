/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  // Force HTTPS in production (HSTS header via reverse proxy recommended)
  poweredByHeader: false,
};

export default nextConfig;
