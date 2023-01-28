declare class EasyMDE {
    public value(newValue: string): void;
    public value(): string;

    public constructor(options: { toolbar: boolean, spellChecker: boolean, status: boolean, indentWithTabs: boolean, autoDownloadFontAwesome: boolean, forceSync: boolean });
}

var easyMDE: EasyMDE;

document.addEventListener("DOMContentLoaded", () => {
    easyMDE = new EasyMDE({ toolbar: false, spellChecker: false, status: false, indentWithTabs: false, autoDownloadFontAwesome: false, forceSync: true });

    let searchBookButton = document.getElementById("search-book") as HTMLInputElement;
    let isbnInput = document.getElementById("isbn") as HTMLInputElement;

    let bookRadio = document.getElementById("type-book") as HTMLInputElement;
    let magazineRadio = document.getElementById("type-magazine") as HTMLInputElement;

    isbnInput.addEventListener("input", () => {
        isbnInput.value = isbnInput.value.replace("-", "").replace("\t", "").trim();
        isbnInput.reportValidity();
        if (bookRadio.checked) {
            searchBookButton.disabled = !isbnInput.value || !isbnInput.checkValidity();
        }
    });

    let nameInput = document.getElementById("name") as HTMLInputElement;
    let numberInput = document.getElementById("number") as HTMLInputElement;
    nameInput.addEventListener("input", () => {
        if (magazineRadio.checked) {
            searchBookButton.disabled = !nameInput.value || !numberInput.value;
        }
    });
    numberInput.addEventListener("input", () => {
        if (magazineRadio.checked) {
            searchBookButton.disabled = !nameInput.value || !numberInput.value;
        }
    });

    searchBookButton.addEventListener("click", () => {
        if (bookRadio.checked) {
            SearchInfo(`/bookinfo/${isbnInput.value}`, "Book");
        }
        else if (magazineRadio.checked) {
            SearchInfo(`/magazineinfo/${nameInput.value}/${numberInput.value}`, "Magazine");
        }
        else {
            alert("Type inconnu");
        }
    });

    document.getElementById("coverUrl").addEventListener("input", () => SetCoverImage((document.getElementById("coverUrl") as HTMLInputElement).value));

    bookRadio.addEventListener("change", () => SwitchToType(bookRadio.value));
    if (bookRadio.checked) {
        SwitchToType(bookRadio.value);
    }
    magazineRadio.addEventListener("change", () => SwitchToType(magazineRadio.value));
    if (magazineRadio.checked) {
        SwitchToType(magazineRadio.value);
    }
});

function SwitchToType(type: string) {

    if (type != "book" && type != "magazine") {
        alert(`Type ${type} unknown!`);
    }

    let showBook = type === "book";

    const isbnElement = document.getElementById("isbn");
    isbnElement.hidden = !showBook;
    if (showBook) {
        isbnElement.focus();
    }
    (document.querySelector(`label[for='isbn']`) as HTMLLabelElement).hidden = !showBook;

    document.getElementById("author").hidden = !showBook;
    (document.querySelector(`label[for='author']`) as HTMLLabelElement).hidden = !showBook;

    document.getElementById("editor").hidden = !showBook;
    (document.querySelector(`label[for='editor']`) as HTMLLabelElement).hidden = !showBook;

    document.getElementById("name").hidden = showBook;
    (document.querySelector(`label[for='name']`) as HTMLLabelElement).hidden = showBook;

    document.getElementById("number").hidden = showBook;
    (document.querySelector(`label[for='number']`) as HTMLLabelElement).hidden = showBook;

    (document.getElementById("search-book") as HTMLInputElement).disabled = true;
}

function SetCoverImage(coverUrl: string) {
    if (/\/img\/[0-9]{13}\.[a-z]{3,4}/.test(coverUrl)) {
        coverUrl = "https://readinglist.laedit.net" + coverUrl;
    }
    document.getElementById("cover-img").setAttribute("src", coverUrl);
}

async function SearchInfo(url: string, name: string) {
    let formElements = Array.from(document.getElementById("submit-book-form").children);
    formElements.forEach(element => (element as HTMLInputElement).disabled = true);

    // loader
    let searchBookButton = document.getElementById("search-book");
    searchBookButton.firstChild.remove();
    var dotFlashingDiv = document.createElement("div");
    dotFlashingDiv.classList.add("dot-flashing");
    searchBookButton.appendChild(dotFlashingDiv);

    try {
        let rawResponse = await fetch(url, { credentials: "same-origin" });
        let response = await rawResponse.json();
        if (response.status === "ok") {
            console.info(response);
            (document.getElementById("title") as HTMLInputElement).value = response.result.title ?? "";
            (document.getElementById("author") as HTMLInputElement).value = response.result.author ?? "";
            (document.getElementById("editor") as HTMLInputElement).value = response.result.editor ?? "";
            (document.getElementById("coverUrl") as HTMLInputElement).value = response.result.coverUrl ?? "";
            SetCoverImage(response.result.coverUrl);
            if (response.result.summary) {
                easyMDE.value(response.result.summary);
            }
        }
        else if (response.status === "error") {
            console.error(response);
            DisplayMessage(`${name} not found`, "warning");
        }
        else {
            console.error(response);
            DisplayMessage(response.message + "\r\n\r\n" + response.details, "error");
        }
    }
    catch (error) {
        console.error(error);
        DisplayMessage(error, "error");
    }

    searchBookButton.firstChild.remove();
    searchBookButton.appendChild(document.createTextNode("Search"));
    formElements.forEach(element => (element as HTMLInputElement).disabled = false);
}

function DisplayMessage(message: string, type: "error" | "warning" | "success") {
    var infoDiv = document.createElement("div");
    infoDiv.classList.add("info");
    infoDiv.classList.add(type);
    infoDiv.append(message);
    document.getElementsByTagName("main")[0].insertBefore(infoDiv, document.getElementById("submit-book-form"));
}
