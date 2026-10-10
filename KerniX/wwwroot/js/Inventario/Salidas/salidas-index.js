(() => {

    const dataNode =
        document.getElementById(
            "kxSalidaChartData"
        );

    let raw = [];

    try {
        raw =
            dataNode
                ? JSON.parse(
                    dataNode.textContent || "[]"
                )
                : [];
    }
    catch {
        raw = [];
    }


    const data =
        Array.isArray(raw)
            ? raw
            : [];


    // =====================================================
    // 1. SERIE DIARIA
    // =====================================================

    const byDate =
        new Map();


    data.forEach(item => {

        const key =
            item.fecha || "Sin fecha";


        if (!byDate.has(key)) {
            byDate.set(
                key,
                {
                    label:
                        item.fechaTexto || key,
                    value:
                        0
                }
            );
        }


        byDate.get(key).value +=
            Number(item.cantidad || 0);
    });


    const dateSeries =
        Array.from(
            byDate.values()
        );


    renderLineChart(
        dateSeries
    );


    // =====================================================
    // 2. TIPOS
    // =====================================================

    const byType =
        new Map();


    data.forEach(item => {

        const key =
            item.tipo || "Entrada";


        byType.set(
            key,
            (byType.get(key) || 0)
            +
            Number(item.cantidad || 0)
        );
    });


    renderDonut(
        Array.from(
            byType.entries()
        )
            .map(
                ([name, value]) => ({
                    name,
                    value
                })
            )
    );


    // =====================================================
    // 3. ALMACENES
    // =====================================================

    const byWarehouse =
        new Map();


    data.forEach(item => {

        const key =
            item.almacen || "Sin almacén";


        byWarehouse.set(
            key,
            (byWarehouse.get(key) || 0)
            +
            Number(item.cantidad || 0)
        );
    });


    renderWarehouseBars(
        Array.from(
            byWarehouse.entries()
        )
            .map(
                ([name, value]) => ({
                    name,
                    value
                })
            )
            .sort(
                (a, b) =>
                    b.value - a.value
            )
            .slice(
                0,
                6
            )
    );


    // =====================================================
    // LINE CHART
    // =====================================================

    function renderLineChart(series) {

        const line =
            document.getElementById(
                "kxLinePath"
            );

        const area =
            document.getElementById(
                "kxAreaPath"
            );

        const pointsGroup =
            document.getElementById(
                "kxChartPoints"
            );

        const axisX =
            document.getElementById(
                "kxAxisX"
            );

        const axisY =
            document.getElementById(
                "kxAxisY"
            );

        const tooltip =
            document.getElementById(
                "kxChartTooltip"
            );

        const wrap =
            document.getElementById(
                "kxLineChartWrap"
            );


        if (
            !line ||
            !area ||
            !pointsGroup ||
            !axisX ||
            !axisY ||
            !tooltip ||
            !wrap
        ) {
            return;
        }


        const values =
            series.length > 0
                ? series
                : [
                    { label: "-", value: 0 },
                    { label: "-", value: 0 }
                ];


        const width =
            700;

        const height =
            220;

        const max =
            Math.max(
                1,
                ...values.map(
                    x =>
                        Number(x.value || 0)
                )
            );


        const niceMax =
            Math.ceil(
                max / 5
            ) * 5
            || 5;


        const xStep =
            values.length <= 1
                ? width
                : width /
                (values.length - 1);


        const coords =
            values.map(
                (item, index) => {

                    const x =
                        values.length <= 1
                            ? width / 2
                            : index * xStep;


                    const y =
                        height -
                        (
                            Number(item.value || 0)
                            /
                            niceMax
                        )
                        *
                        (height - 12);


                    return {
                        x,
                        y,
                        value:
                            Number(item.value || 0),
                        label:
                            item.label || "-"
                    };
                }
            );


        const path =
            coords
                .map(
                    (p, i) =>
                        `${i === 0 ? "M" : "L"}${p.x.toFixed(2)},${p.y.toFixed(2)}`
                )
                .join(" ");


        line.setAttribute(
            "d",
            path
        );


        const areaPath =
            path
            +
            ` L${coords[coords.length - 1].x},${height}`
            +
            ` L${coords[0].x},${height} Z`;


        area.setAttribute(
            "d",
            areaPath
        );


        // Y axis
        axisY.innerHTML = "";

        for (
            let i = 4;
            i >= 0;
            i--
        ) {
            const span =
                document.createElement(
                    "span"
                );

            span.textContent =
                formatQty(
                    niceMax * i / 4
                );

            axisY.appendChild(
                span
            );
        }


        // X axis
        axisX.innerHTML = "";


        const labelsToShow =
            Math.min(
                values.length,
                7
            );


        if (labelsToShow > 0) {

            for (
                let i = 0;
                i < labelsToShow;
                i++
            ) {

                const sourceIndex =
                    labelsToShow === 1
                        ? 0
                        : Math.round(
                            i *
                            (values.length - 1)
                            /
                            (labelsToShow - 1)
                        );


                const span =
                    document.createElement(
                        "span"
                    );

                span.textContent =
                    values[sourceIndex].label;

                axisX.appendChild(
                    span
                );
            }
        }


        // points
        pointsGroup.innerHTML = "";


        coords.forEach(
            point => {

                const circle =
                    document.createElementNS(
                        "http://www.w3.org/2000/svg",
                        "circle"
                    );


                circle.setAttribute(
                    "cx",
                    point.x
                );

                circle.setAttribute(
                    "cy",
                    point.y
                );

                circle.setAttribute(
                    "r",
                    "4.5"
                );

                circle.setAttribute(
                    "fill",
                    "#05111f"
                );

                circle.setAttribute(
                    "stroke",
                    "#19d8ff"
                );

                circle.setAttribute(
                    "stroke-width",
                    "2.5"
                );

                circle.style.cursor =
                    "pointer";

                circle.style.filter =
                    "drop-shadow(0 0 5px #19d8ff)";


                circle.addEventListener(
                    "mouseenter",
                    () => {

                        tooltip.innerHTML =
                            `<strong>${escapeHtml(point.label)}</strong><br>` +
                            `-${formatQty(point.value)} unidades`;


                        const svgRect =
                            line.ownerSVGElement
                                .getBoundingClientRect();


                        const wrapRect =
                            wrap.getBoundingClientRect();


                        const left =
                            (
                                point.x / width
                            )
                            *
                            svgRect.width
                            +
                            (
                                svgRect.left -
                                wrapRect.left
                            );


                        const top =
                            (
                                point.y / height
                            )
                            *
                            svgRect.height
                            +
                            (
                                svgRect.top -
                                wrapRect.top
                            );


                        tooltip.style.left =
                            `${left}px`;

                        tooltip.style.top =
                            `${top}px`;

                        tooltip.style.opacity =
                            "1";
                    }
                );


                circle.addEventListener(
                    "mouseleave",
                    () => {

                        tooltip.style.opacity =
                            "0";
                    }
                );


                pointsGroup.appendChild(
                    circle
                );
            }
        );
    }


    // =====================================================
    // DONUT
    // =====================================================

    function renderDonut(series) {

        const donut =
            document.getElementById(
                "kxTypeDonut"
            );

        const legend =
            document.getElementById(
                "kxTypeLegend"
            );

        const totalEl =
            document.getElementById(
                "kxDonutTotal"
            );


        if (
            !donut ||
            !legend ||
            !totalEl
        ) {
            return;
        }


        const colors = [
            "#ffbf2f",
            "#19d8ff",
            "#6d4aff",
            "#34efb1",
            "#ff6177",
            "#ff9f43"
        ];


        const clean =
            series
                .filter(
                    x =>
                        Number(x.value || 0) > 0
                )
                .slice(
                    0,
                    6
                );


        const total =
            clean.reduce(
                (sum, x) =>
                    sum +
                    Number(x.value || 0),
                0
            );


        totalEl.textContent =
            formatQty(
                total
            );


        if (total <= 0) {

            donut.style.background =
                "conic-gradient(#263a52 0 100%)";

            legend.innerHTML =
                `<div class="kx-secondary">Sin datos para graficar.</div>`;

            return;
        }


        let cursor =
            0;

        const stops =
            [];


        clean.forEach(
            (item, index) => {

                const share =
                    Number(item.value || 0)
                    /
                    total
                    *
                    100;


                const start =
                    cursor;

                const end =
                    cursor +
                    share;


                stops.push(
                    `${colors[index % colors.length]} ${start.toFixed(2)}% ${end.toFixed(2)}%`
                );


                cursor =
                    end;
            }
        );


        donut.style.background =
            `conic-gradient(${stops.join(",")})`;


        legend.innerHTML =
            "";


        clean.forEach(
            (item, index) => {

                const pct =
                    Number(item.value || 0)
                    /
                    total
                    *
                    100;


                const row =
                    document.createElement(
                        "div"
                    );

                row.className =
                    "kx-legend-item";


                row.innerHTML =
                    `
                            <span class="kx-legend-dot"
                                  style="color:${colors[index % colors.length]};background:${colors[index % colors.length]}">
                            </span>

                            <span class="kx-legend-name"
                                  title="${escapeHtml(item.name)}">
                                ${escapeHtml(item.name)}
                            </span>

                            <span class="kx-legend-value">
                                ${pct.toFixed(1)}%
                            </span>
                            `;


                legend.appendChild(
                    row
                );
            }
        );
    }


    // =====================================================
    // ALMACENES
    // =====================================================

    function renderWarehouseBars(series) {

        const host =
            document.getElementById(
                "kxWarehouseBars"
            );


        if (!host) {
            return;
        }


        host.innerHTML =
            "";


        if (
            !series ||
            series.length === 0
        ) {

            host.innerHTML =
                `<div class="kx-secondary">Sin datos para graficar.</div>`;

            return;
        }


        const max =
            Math.max(
                ...series.map(
                    x =>
                        Number(x.value || 0)
                ),
                1
            );


        series.forEach(
            item => {

                const pct =
                    Math.max(
                        3,
                        Number(item.value || 0)
                        /
                        max
                        *
                        100
                    );


                const row =
                    document.createElement(
                        "div"
                    );

                row.className =
                    "kx-bar-item";


                row.innerHTML =
                    `
                            <div class="kx-bar-top">
                                <span class="kx-bar-name"
                                      title="${escapeHtml(item.name)}">
                                    ${escapeHtml(item.name)}
                                </span>

                                <span class="kx-bar-val">
                                    +${formatQty(item.value)}
                                </span>
                            </div>

                            <div class="kx-bar-track">
                                <div class="kx-bar-fill"
                                     style="width:0"
                                     data-width="${pct.toFixed(2)}%">
                                </div>
                            </div>
                            `;


                host.appendChild(
                    row
                );
            }
        );


        requestAnimationFrame(
            () => {

                host
                    .querySelectorAll(
                        ".kx-bar-fill"
                    )
                    .forEach(
                        bar => {

                            bar.style.width =
                                bar.dataset.width ||
                                "0";
                        }
                    );
            }
        );
    }


    // =====================================================
    // UTILS
    // =====================================================

    function formatQty(value) {

        const number =
            Number(value || 0);


        return Number.isInteger(number)
            ? number.toString()
            : number.toFixed(2)
                .replace(/0+$/, "")
                .replace(/\.$/, "");
    }


    function escapeHtml(value) {

        return String(
            value ?? ""
        )
            .replace(
                /&/g,
                "&amp;"
            )
            .replace(
                /</g,
                "&lt;"
            )
            .replace(
                />/g,
                "&gt;"
            )
            .replace(
                /"/g,
                "&quot;"
            )
            .replace(
                /'/g,
                "&#039;"
            );
    }

})();