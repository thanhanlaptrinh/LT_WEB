param([string]$BaseUrl = "http://localhost:5080")
$ErrorActionPreference = "Stop"
$paths = @("/", "/Home/New", "/Orders/Lookup", "/Orders/Checkout",
    "/Identity/Account/Login", "/Identity/Account/Register",
    "/css/bootstrap.min.css", "/css/flower-shop.css", "/css/auth.css",
    "/js/bootstrap.bundle.min.js", "/js/flower-shop.js", "/js/auth.js",
    "/js/storefront.js", "/css/storefront.css", "/fonts/lanu-regular.woff", "/fonts/lanu-bold.woff",
    "/Products/BestSellers")
$failed = 0
foreach ($path in $paths) {
    try {
        $response = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd('/') + $path) -UseBasicParsing
        if ($response.StatusCode -ne 200) { throw "HTTP $($response.StatusCode)" }
        if ($path.EndsWith(".css") -and $response.Headers['Content-Type'] -notmatch "text/css") { throw "CSS returned wrong Content-Type" }
        if ($path.EndsWith(".js") -and $response.Headers['Content-Type'] -notmatch "javascript") { throw "JS returned wrong Content-Type" }
        if ($path -match '/Identity/Account/(Login|Register)' -and $response.Content -notmatch '/css/auth.css') { throw "Shared authentication stylesheet missing" }
        Write-Host "PASS $path" -ForegroundColor Green
    } catch {
        $failed++
        Write-Host "FAIL $path : $($_.Exception.Message)" -ForegroundColor Red
    }
}
if ($failed -gt 0) { Write-Host "$failed check(s) failed. Keep the app running and inspect its terminal log."; exit 1 }
Write-Host "All public page/asset checks passed. Login, checkout and admin workflows still require manual testing."
