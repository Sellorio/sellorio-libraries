const states = new WeakMap();

export function initialize(root, dotNetReference, backToTopVisibilityOffset) {
	const state = {
		dotNetReference,
		backToTopVisibilityOffset,
		visiblePages: new Set(),
		lastVisibleRange: null,
		scrollHandler: null,
		observer: null
	};

	state.scrollHandler = () => {
		state.dotNetReference.invokeMethodAsync(
			"SetBackToTopVisibleAsync",
			window.scrollY >= state.backToTopVisibilityOffset);
	};

	state.observer = new IntersectionObserver(entries => {
		let visibleRangeChanged = false;

		for (const entry of entries) {
			const pageNumber = Number.parseInt(entry.target.dataset.pageNumber ?? "", 10);

			if (Number.isNaN(pageNumber)) {
				continue;
			}

			if (entry.isIntersecting) {
				state.visiblePages.add(pageNumber);
			} else {
				state.visiblePages.delete(pageNumber);
			}

			if (entry.target.dataset.loaded === "true") {
				const height = Math.ceil(entry.target.getBoundingClientRect().height);

				if (height > 0) {
					state.dotNetReference.invokeMethodAsync("SetPageHeightAsync", pageNumber, height);
				}
			}

			visibleRangeChanged = true;
		}

		if (visibleRangeChanged && state.visiblePages.size > 0) {
			const pageNumbers = [...state.visiblePages].sort((left, right) => left - right);
			const range = `${pageNumbers[0]}:${pageNumbers[pageNumbers.length - 1]}`;

			if (state.lastVisibleRange !== range) {
				state.lastVisibleRange = range;
				state.dotNetReference.invokeMethodAsync(
					"UpdateVisibleRangeAsync",
					pageNumbers[0],
					pageNumbers[pageNumbers.length - 1]);
			}
		}
	}, {
		rootMargin: "1000px 0px 1000px 0px",
		threshold: 0
	});

	window.addEventListener("scroll", state.scrollHandler, { passive: true });
	states.set(root, state);
	state.scrollHandler();
}

export function refresh(root) {
	const state = states.get(root);

	if (!state) {
		return;
	}

	state.visiblePages.clear();
	state.lastVisibleRange = null;
	state.observer.disconnect();

	for (const pageElement of root.querySelectorAll("[data-page-number]")) {
		state.observer.observe(pageElement);

		if (pageElement.dataset.loaded === "true") {
			const pageNumber = Number.parseInt(pageElement.dataset.pageNumber ?? "", 10);
			const height = Math.ceil(pageElement.getBoundingClientRect().height);

			if (!Number.isNaN(pageNumber) && height > 0) {
				state.dotNetReference.invokeMethodAsync("SetPageHeightAsync", pageNumber, height);
			}
		}
	}
}

export function scrollToTop() {
	window.scrollTo({ top: 0, behavior: "smooth" });
}

export function dispose(root) {
	const state = states.get(root);

	if (!state) {
		return;
	}

	state.observer.disconnect();
	window.removeEventListener("scroll", state.scrollHandler);
	states.delete(root);
}
