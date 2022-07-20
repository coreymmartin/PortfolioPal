// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function CheckMarketOpen(){
    var test = 1;
    var marketLabl = "Market ";
    marketLabl += (test > 0) ? "Open" : "Closed";
    return marketLabl;
}