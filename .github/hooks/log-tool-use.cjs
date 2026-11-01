const fs = require('node:fs');
const path = require('node:path');

try {
  const event = JSON.parse(fs.readFileSync(0, 'utf8'));

  const tool = event.toolName || event.tool_name;

  const entry = {
    timestamp: new Date().toISOString(),
    event: tool ? 'preToolUse' : 'sessionStart',
    ...(tool ? { tool } : {})
  };

  fs.appendFileSync(
    path.join(__dirname, 'tool-use.log'),
    JSON.stringify(entry) + '\n'
  );

  process.stdout.write('{}');
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}