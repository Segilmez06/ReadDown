document.querySelectorAll('a').forEach(function (item) {
    let link = item.href;
    if (link.startsWith('http')) {
        item.href = `javascript:window.open('${link}', '_blank');`;
    }
});
