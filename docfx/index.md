---
title: ようこそ
---

<script src="https://cdn.jsdelivr.net/npm/typeit@8.7.1/dist/index.umd.js"></script>
<link href="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/themes/prism-tomorrow.min.css" rel="stylesheet" />
<script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/prism.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-csharp.min.js"></script>

<style>
  .half-width {
    width: 50%;
    background: #1e1e1e;
    color: #dcdcdc;
    padding: 1em;
    border-radius: 6px;
    overflow-x: auto;
  }
  .affix {
    display: none !important;
  }
</style>

<script>
window.addEventListener('DOMContentLoaded', () => {
  new TypeIt("#typing-box1", {
    speed: 15,
    waitUntilVisible: true,
    lifeLike: true,
    cursor: false,
    afterComplete: () => {
      Prism.highlightElement(document.getElementById("typing-box1"));
    }
  })
  .type("dotnet add package McpX")
  .go();
});

window.addEventListener('DOMContentLoaded', () => {
  new TypeIt("#typing-box2", {
    speed: 15,
    waitUntilVisible: true,
    lifeLike: true,
    cursor: false,
    afterComplete: () => {
      Prism.highlightElement(document.getElementById("typing-box2"));
    }
  })
  .type("using McpXLib;\n")
  .type("using McpXLib.Enums;\n\n")
  .type('using (var mcpx = new McpX("192.168.12.88", 10000)){\n')
  .type('   mcpx.Write(Prefix.D, "0", 1234);\n')
  .type('   var value = mcpx.Read(Prefix.D, "0");\n')
  .type('}')
  .go();
});
</script>

<div style="text-align: center;">
  <img src="images/logo.svg" alt="logo" />
</div>

<div style="max-width: 800px; margin: 0 auto; text-align: center;">
  <div style="margin: 2em auto 1em auto;">
    <img alt="Version" src="https://img.shields.io/badge/version-0.4.2-blue" />
    <img alt=".NET 7.0+" src="https://img.shields.io/badge/.NET-7.0+-blueviolet" />
    <img alt=".NET 8.0+" src="https://img.shields.io/badge/.NET-8.0+-purple" />
    <img alt=".NET 9.0+" src="https://img.shields.io/badge/.NET-9.0+-indigo" />
    <img alt=".NET Core 2.0+" src="https://img.shields.io/badge/.NET_Core-2.0+-darkgreen" />
    <img alt=".NET Framework 4.6.1+" src="https://img.shields.io/badge/.NET_Framework-4.6.1+-teal?logo=windows" />
    <img alt="License" src="https://img.shields.io/badge/license-MIT-brightgreen.svg" />
  </div>
  <div style="text-align: left;">
    <p style="margin: 2em 0 1em 0;">
      McpXは、三菱電機製PLCと通信するためのMCプロトコル対応ライブラリです。<br>
      シンプルなAPIで扱いやすく、MCプロトコルを意識することなく利用でき、Linux、Windows、macOS など、さまざまなプラットフォームで動作します。
    </p>
    <p style="margin: 2em 0 1em 0;">
      <strong>NuGetでインストール</strong>
    </p>
  </div>

  <pre id="typing-box1" class="language-shell"
    style="max-width: 800px; min-height:20px; margin: 1em auto; background:#1e1e1e; color:#dcdcdc; padding:1em; border-radius:6px;">
  </pre>
  
  <div style="text-align: left;">
    <p style="margin: 2em 0 1em 0;">
      <strong>数行のコードでPLCのデバイスにアクセス可能です！</strong>
    </p>
  </div>

  <pre id="typing-box2" class="language-csharp"
    style="max-width: 800px; min-height:200px; margin: 1em auto; background:#1e1e1e; color:#dcdcdc; padding:1em; border-radius:6px;">
  </pre>

  <div style="margin: 2em auto; text-align: left;">
    <ul>
      <li><a href="docs/introduction.html">インストール方法</a></li>
      <li><a href="docs/plc_parameter_setting.html">PLCパラメータ設定</a></li>
      <li><a href="api/McpXLib.McpX.html">APIドキュメント</a></li>
      <li><a href="https://github.com/YudaiKitamura/McpX">GitHub</a></li>
      <li><a href="https://www.nuget.org/packages/McpX/">NuGet</a></li>
    </ul>
  </div>
</div>
