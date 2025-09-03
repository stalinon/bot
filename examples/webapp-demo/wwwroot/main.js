document.addEventListener('DOMContentLoaded', async () => {
    // Получаем объект WebApp
    const tg = window.Telegram.WebApp;

    // Запрашиваем JWT с сервера
    try {
        const resp = await fetch(`/webapp/auth?initData=${encodeURIComponent(tg.initData)}`);
        if (resp.ok) {
            const jwt = await resp.text();
            console.log('JWT', jwt);
        } else {
            console.error('Не удалось получить JWT');
        }
    } catch (e) {
        console.error('Ошибка при запросе JWT', e);
    }

    // Отправляем данные боту
    const form = document.getElementById('data-form');
    const input = document.getElementById('data-input');
    form.addEventListener('submit', (ev) => {
        ev.preventDefault();
        tg.sendData(JSON.stringify({ text: input.value }));
    });
});
