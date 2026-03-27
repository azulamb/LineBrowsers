const EXE = "bin/LineBrowsers.exe";

// 1. Get version from exe
const ps = await new Deno.Command("powershell", {
  args: [
    "-NoProfile",
    "-NonInteractive",
    "-Command",
    `(Get-Item '${EXE}').VersionInfo.ProductVersion`,
  ],
}).output();

if (!ps.success) {
  console.error(`Failed to read version from ${EXE}`);
  Deno.exit(1);
}

const version = new TextDecoder().decode(ps.stdout).trim().split("+")[0];
if (!version) {
  console.error("Version is empty");
  Deno.exit(1);
}
console.log(`Version: ${version}`);

// 2. Check if tag v{version} already exists
const tagList = await new Deno.Command("git", {
  args: ["tag", "-l", `v${version}`],
}).output();

const existing = new TextDecoder().decode(tagList.stdout).trim();
if (existing) {
  console.error(`Error: tag v${version} already exists`);
  Deno.exit(1);
}

console.log(`OK: v${version} is ready to release`);
