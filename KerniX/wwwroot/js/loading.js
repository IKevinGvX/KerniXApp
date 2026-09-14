"use strict";

document.addEventListener("DOMContentLoaded", () => {

    const progressBar =
        document.getElementById("kxProgressBar");

    const progressLight =
        document.getElementById("kxProgressLight");

    const percentage =
        document.getElementById("kxPercentage");

    const loadingText =
        document.getElementById("kxLoadingText");

    const steps =
        Array.from(
            document.querySelectorAll(".kx-step")
        );

    const dashboardUrl =
        document.body.dataset.dashboardUrl?.trim()
        || "/Inicio/Index";


    // =========================================================
    // ETAPAS REALES DEL ALCANCE
    // =========================================================

    const stages = [

        {
            start: 0,
            end: 20,
            text: "Verificando sesión segura..."
        },

        {
            start: 20,
            end: 40,
            text: "Cargando permisos del usuario..."
        },

        {
            start: 40,
            end: 65,
            text: "Sincronizando ventas e inventario..."
        },

        {
            start: 65,
            end: 85,
            text: "Preparando indicadores y reportes..."
        },

        {
            start: 85,
            end: 100,
            text: "Inicializando KerniX AI..."
        }

    ];


    let progress = 0;
    let timer = null;
    let redirecting = false;


    // =========================================================
    // PROGRESO
    // =========================================================

    function renderProgress() {

        const value =
            Math.max(
                0,
                Math.min(progress, 100)
            );

        if (progressBar) {

            progressBar.style.width =
                `${value}%`;

            progressBar.setAttribute(
                "aria-valuenow",
                value.toString()
            );
        }

        if (progressLight) {

            progressLight.style.left =
                `calc(${Math.min(value, 98)}% - 4px)`;
        }

        if (percentage) {

            percentage.textContent =
                `${value}%`;
        }

        updateCurrentStage();

        updateSteps();
    }


    // =========================================================
    // TEXTO DE ETAPA
    // =========================================================

    function updateCurrentStage() {

        if (!loadingText) {
            return;
        }

        if (progress >= 100) {

            loadingText.textContent =
                "Centro de operaciones preparado.";

            return;
        }

        const stage =
            stages.find(
                item =>
                    progress >= item.start &&
                    progress < item.end
            );

        if (stage) {

            loadingText.textContent =
                stage.text;
        }
    }


    // =========================================================
    // PASOS
    // =========================================================

    function updateSteps() {

        steps.forEach(
            (step, index) => {

                const stage =
                    stages[index];

                if (!stage) {
                    return;
                }

                const status =
                    step.querySelector(
                        ".kx-step-status"
                    );

                step.classList.remove(
                    "active",
                    "completed",
                    "waiting"
                );


                if (progress >= stage.end) {

                    step.classList.add(
                        "completed"
                    );

                    if (status) {
                        status.textContent =
                            "Completado";
                    }

                    return;
                }


                if (
                    progress >= stage.start &&
                    progress < stage.end
                ) {

                    step.classList.add(
                        "active"
                    );

                    if (status) {
                        status.textContent =
                            "En proceso...";
                    }

                    return;
                }


                step.classList.add(
                    "waiting"
                );

                if (status) {
                    status.textContent =
                        "Esperando...";
                }
            }
        );
    }


    // =========================================================
    // COMPLETAR TODO
    // =========================================================

    function completeAllSteps() {

        steps.forEach(step => {

            step.classList.remove(
                "active",
                "waiting"
            );

            step.classList.add(
                "completed"
            );

            const status =
                step.querySelector(
                    ".kx-step-status"
                );

            if (status) {
                status.textContent =
                    "Completado";
            }
        });
    }


    // =========================================================
    // REDIRECCIÓN
    // =========================================================

    function redirectToDashboard() {

        if (redirecting) {
            return;
        }

        redirecting = true;

        completeAllSteps();

        if (percentage) {
            percentage.textContent =
                "100%";
        }

        if (loadingText) {

            loadingText.textContent =
                "KerniX está listo.";
        }


        window.setTimeout(() => {

            window.location.assign(
                dashboardUrl
            );

        }, 850);
    }
    // =====================================================
    // REDIRECCIÓN AL DASHBOARD
    // =====================================================

    setTimeout(() => {
        window.location.href = "/Inicio/Index";
    }, 700);

    // =========================================================
    // VELOCIDAD
    // =========================================================

    function getIncrement() {

        if (progress < 25) {
            return 3;
        }

        if (progress < 65) {
            return 2;
        }

        return 1;
    }


    // =========================================================
    // INICIAR
    // =========================================================

    function startLoading() {

        renderProgress();

        timer =
            window.setInterval(() => {

                progress +=
                    getIncrement();

                if (progress >= 100) {

                    progress = 100;

                    renderProgress();

                    window.clearInterval(
                        timer
                    );

                    timer = null;

                    redirectToDashboard();

                    return;
                }

                renderProgress();

            }, 75);
    }


    startLoading();

});