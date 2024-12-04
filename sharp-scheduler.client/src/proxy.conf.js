const PROXY_CONFIG = [
  {
    context: [
      "/api/",
      "/api/Account/",
      "/api/Account/login"
    ],
    target: "https://localhost:7286",
    secure: false
  }
]

module.exports = PROXY_CONFIG;
