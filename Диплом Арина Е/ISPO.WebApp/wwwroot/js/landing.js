(() => {
  const revealNodes = document.querySelectorAll(".reveal");
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("visible");
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.2 }
  );

  revealNodes.forEach((node) => observer.observe(node));

  const counters = document.querySelectorAll(".count-up");
  const countObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) {
          return;
        }

        const el = entry.target;
        const target = Number(el.getAttribute("data-target")) || 0;
        const durationMs = 1200;
        const start = performance.now();

        const tick = (timestamp) => {
          const progress = Math.min((timestamp - start) / durationMs, 1);
          const value = Math.floor(progress * target);
          el.textContent = value.toLocaleString("ru-RU");
          if (progress < 1) {
            requestAnimationFrame(tick);
          } else {
            el.textContent = target.toLocaleString("ru-RU");
          }
        };

        requestAnimationFrame(tick);
        countObserver.unobserve(el);
      });
    },
    { threshold: 0.4 }
  );

  counters.forEach((counter) => countObserver.observe(counter));
})();
