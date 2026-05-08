window.Prism = (() => {
  const escapeHtml = (value) => value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;');

  const wrap = (value, token) => `<span class="token ${token}">${value}</span>`;

  const rules = [
    [/("(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')/g, 'string'],
    [/\b(false|true|null|undefined|class|const|let|var|function|return|if|else|for|while|using|namespace|public|private|sealed|record|new|await|async|try|catch|switch|case|break|continue)\b/g, 'keyword'],
    [/\b(\d+(?:\.\d+)?)\b/g, 'number'],
    [/(\/\/[^\n]*|\/\*[\s\S]*?\*\/|#[^\n]*)/g, 'comment']
  ];

  const highlight = (code) => {
    const placeholders = [];
    let escaped = escapeHtml(code);
    for (const [pattern, token] of rules) {
      escaped = escaped.replace(pattern, (match) => {
        const placeholder = `___PRISM_${placeholders.length}___`;
        placeholders.push(wrap(match, token));
        return placeholder;
      });
    }

    placeholders.forEach((value, index) => {
      escaped = escaped.replace(`___PRISM_${index}___`, value);
    });
    return escaped;
  };

  const highlightAll = () => {
    document.querySelectorAll('pre code').forEach((element) => {
      element.innerHTML = highlight(element.textContent || '');
    });
  };

  return { highlightAll };
})();

