document.addEventListener("DOMContentLoaded", function () {
    const galleryImages = Array.from(document.querySelectorAll(".gallery-clickable"));
    const lightbox = document.getElementById("galleryLightbox");
    const lightboxImage = document.getElementById("galleryLightboxImage");
    const closeBtn = document.getElementById("galleryCloseBtn");
    const prevBtn = document.getElementById("galleryPrevBtn");
    const nextBtn = document.getElementById("galleryNextBtn");

    let currentImageIndex = 0;

    function showImage(index) {
        if (!galleryImages.length || !lightbox || !lightboxImage) return;

        currentImageIndex = index;
        lightboxImage.src = galleryImages[index].getAttribute("data-full");
        lightboxImage.alt = galleryImages[index].alt || "Снимка";
        lightbox.classList.add("open");
        document.body.classList.add("lightbox-open");
    }

    function closeLightbox() {
        if (!lightbox) return;
        lightbox.classList.remove("open");
        document.body.classList.remove("lightbox-open");
    }

    function showPrev() {
        if (!galleryImages.length) return;
        currentImageIndex = (currentImageIndex - 1 + galleryImages.length) % galleryImages.length;
        showImage(currentImageIndex);
    }

    function showNext() {
        if (!galleryImages.length) return;
        currentImageIndex = (currentImageIndex + 1) % galleryImages.length;
        showImage(currentImageIndex);
    }

    galleryImages.forEach((img, index) => {
        img.addEventListener("click", function () {
            showImage(index);
        });
    });

    if (closeBtn) closeBtn.addEventListener("click", closeLightbox);
    if (prevBtn) prevBtn.addEventListener("click", showPrev);
    if (nextBtn) nextBtn.addEventListener("click", showNext);

    if (lightbox) {
        lightbox.addEventListener("click", function (e) {
            if (e.target === lightbox) {
                closeLightbox();
            }
        });
    }

    document.addEventListener("keydown", function (e) {
        if (!lightbox || !lightbox.classList.contains("open")) return;

        if (e.key === "Escape") closeLightbox();
        if (e.key === "ArrowLeft") showPrev();
        if (e.key === "ArrowRight") showNext();
    });
});