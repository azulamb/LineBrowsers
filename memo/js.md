# JS注入メモ

## Xのタイムライン自動更新

```js
(() => {
  if (window.__xHomeClicker) {
    clearInterval(window.__xHomeClicker);
  }

  window.__xHomeClicker = setInterval(() => {
    if (location.href !== "https://x.com/home") {
      return;
    }
    if (window.scrollY !== 0) {
      return;
    }

    const el = document.querySelectorAll('[href="/home"]')[0];
    if (!el) {
      return;
    }

    el.click();
  }, 60000);
})();
```
