document.addEventListener("DOMContentLoaded", () => {

    const progress =
        document.getElementById("welcomeProgress");

    const percentage =
        document.getElementById("welcomePercentage");

    const status =
        document.getElementById("welcomeStatus");


    if (!progress ||
        !percentage ||
        !status) {
        return;
    }


    const estados = [
        {
            max: 22,
            texto: "Preparando sesión..."
        },
        {
            max: 46,
            texto: "Cargando información..."
        },
        {
            max: 68,
            texto: "Cargando módulos..."
        },
        {
            max: 88,
            texto: "Preparando inventario y ventas..."
        },
        {
            max: 100,
            texto: "Espacio de trabajo preparado."
        }
    ];


    let valor = 0;


    function obtenerEstado(porcentaje) {

        return estados.find(
            e => porcentaje <= e.max
        ) ?? estados[estados.length - 1];

    }


    const intervalo =
        window.setInterval(() => {

            valor += Math.floor(
                Math.random() * 4
            ) + 1;


            if (valor >= 100) {

                valor = 100;

                clearInterval(intervalo);

            }


            progress.style.width =
                `${valor}%`;

            percentage.textContent =
                `${valor}%`;

            status.textContent =
                obtenerEstado(valor).texto;

        }, 75);

});