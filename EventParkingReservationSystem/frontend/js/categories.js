import { api } from "./api.js";

import {
    renderNavbar,
    showFeedback,
    hideFeedback,
    setButtonLoading
} from "./ui.js";


const categoryForm =
    document.getElementById("categoryForm");

const categoryIdInput =
    document.getElementById("categoryId");

const categoryNameInput =
    document.getElementById("categoryName");

const categoryDescriptionInput =
    document.getElementById("categoryDescription");

const categoryList =
    document.getElementById("categoryList");

const categoryFormTitle =
    document.getElementById("categoryFormTitle");

const saveCategoryButton =
    document.getElementById("saveCategoryButton");

const cancelEditButton =
    document.getElementById("cancelEditButton");


let categories = [];


document.addEventListener(
    "DOMContentLoaded",
    async () => {
        renderNavbar();
        await loadCategories();
    }
);


async function loadCategories() {
    try {
        hideFeedback();

        categories =
            await api.get("/api/Categories");

        renderCategories();

    } catch (error) {

        categoryList.innerHTML = "";

        showFeedback(
            error.message ||
            "Unable to load categories.",
            "error"
        );
    }
}


function renderCategories() {

    categoryList.innerHTML = "";

    if (!categories || categories.length === 0) {

        showFeedback(
            "No event categories are available.",
            "info"
        );

        return;
    }

    hideFeedback();


    categories.forEach((category) => {

        const card =
            document.createElement("article");

        card.className =
            "service-card";

        card.innerHTML = `
            <div class="service-number">
                #${category.id}
            </div>

            <div class="service-icon">
                C
            </div>

            <h3>
                ${escapeHtml(category.name)}
            </h3>

            <p>
                ${escapeHtml(
                    category.description ||
                    "No description available."
                )}
            </p>

            <div
                class="hero-actions"
                style="margin-top: auto;">

                <button
                    class="btn btn-primary edit-category-button"
                    type="button"
                    data-id="${category.id}">
                    Edit
                </button>

                <button
                    class="btn btn-secondary delete-category-button"
                    type="button"
                    data-id="${category.id}">
                    Delete
                </button>

            </div>
        `;

        categoryList.appendChild(card);
    });


    attachCategoryActionEvents();
}


function attachCategoryActionEvents() {

    document
        .querySelectorAll(
            ".edit-category-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                () => {
                    startEditCategory(
                        Number(button.dataset.id)
                    );
                }
            );
        });


    document
        .querySelectorAll(
            ".delete-category-button"
        )
        .forEach((button) => {

            button.addEventListener(
                "click",
                async () => {
                    await deleteCategory(
                        Number(button.dataset.id)
                    );
                }
            );
        });
}


function startEditCategory(categoryId) {

    const category =
        categories.find(
            (item) =>
                item.id === categoryId
        );

    if (!category) {
        return;
    }


    categoryIdInput.value =
        category.id;

    categoryNameInput.value =
        category.name;

    categoryDescriptionInput.value =
        category.description || "";

    categoryFormTitle.textContent =
        "Edit Category";

    saveCategoryButton.textContent =
        "Update Category";

    cancelEditButton.hidden =
        false;


    categoryForm.scrollIntoView({
        behavior: "smooth",
        block: "start"
    });
}


function resetCategoryForm() {

    categoryForm.reset();

    categoryIdInput.value = "";

    categoryFormTitle.textContent =
        "Add New Category";

    saveCategoryButton.textContent =
        "Save Category";

    cancelEditButton.hidden =
        true;
}


async function saveCategory(event) {

    event.preventDefault();


    const categoryId =
        categoryIdInput.value;

    const categoryData = {

        name:
            categoryNameInput.value.trim(),

        description:
            categoryDescriptionInput.value.trim()
    };


    if (!categoryData.name) {

        showFeedback(
            "Please enter a category name.",
            "error"
        );

        return;
    }


    try {

        setButtonLoading(
            saveCategoryButton,
            true,
            categoryId
                ? "Updating..."
                : "Saving..."
        );

        hideFeedback();


        if (categoryId) {

            await api.put(
                `/api/Categories/${categoryId}`,
                categoryData
            );

            showFeedback(
                "Category updated successfully.",
                "success"
            );

        } else {

            await api.post(
                "/api/Categories",
                categoryData
            );

            showFeedback(
                "Category created successfully.",
                "success"
            );
        }


        resetCategoryForm();

        await loadCategories();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to save category.",
            "error"
        );

    } finally {

        setButtonLoading(
            saveCategoryButton,
            false
        );
    }
}


async function deleteCategory(categoryId) {

    const category =
        categories.find(
            (item) =>
                item.id === categoryId
        );

    if (!category) {
        return;
    }


    const confirmed =
        window.confirm(
            `Delete "${category.name}"?`
        );

    if (!confirmed) {
        return;
    }


    try {

        hideFeedback();

        await api.delete(
            `/api/Categories/${categoryId}`
        );

        showFeedback(
            "Category deleted successfully.",
            "success"
        );

        await loadCategories();

    } catch (error) {

        showFeedback(
            error.message ||
            "Unable to delete category. It may be used by an event.",
            "error"
        );
    }
}


function escapeHtml(value) {

    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}


categoryForm.addEventListener(
    "submit",
    saveCategory
);


cancelEditButton.addEventListener(
    "click",
    () => {
        resetCategoryForm();
        hideFeedback();
    }
);