// Runs before `ng build` to inject environment variables into environment.prod.ts.
// Usage: node set-env.js
// Required env vars: API_URL, FRONTEND_URL
const fs = require('fs');
const path = require('path');

const apiUrl = process.env.API_URL;
const frontendUrl = process.env.FRONTEND_URL;

if (!apiUrl || !frontendUrl) {
  console.error('ERROR: API_URL and FRONTEND_URL environment variables are required.');
  process.exit(1);
}

const filePath = path.join(__dirname, 'src', 'environments', 'environment.prod.ts');
let content = fs.readFileSync(filePath, 'utf8');
content = content
  .replace('__API_URL__', apiUrl.replace(/\/$/, ''))
  .replace('__FRONTEND_URL__', frontendUrl.replace(/\/$/, ''));

fs.writeFileSync(filePath, content, 'utf8');
console.log(`environment.prod.ts written with API_URL=${apiUrl} FRONTEND_URL=${frontendUrl}`);
