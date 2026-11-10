// Návrh username ve formuláři Nový student / Upravit studenta.
// PROČ tady není žádné pravidlo pro username: návrh vždy počítá server (UsernameGenerator).
//   Skript jen pošle jméno a příjmení a výsledek vloží do pole.
//
// Formulář musí mít atributy:
//   id="student-form"
//   data-suggest-url  – adresa handleru SuggestUsername
//   data-email-domain – doména pro náhled UPN

// PROČ složené závorky kolem kódu: proměnné (const, let) platí jen uvnitř
//   a nesrazí se s jinými skripty na stránce.
{
    const form = document.getElementById('student-form');
    const firstNameInput = document.getElementById('Input_FirstName');
    const lastNameInput = document.getElementById('Input_LastName');
    const usernameInput = document.getElementById('Input_Username');
    const suggestButton = document.getElementById('suggest-username');
    const reviewHint = document.getElementById('username-review');
    const upnPreview = document.getElementById('upn-preview');

    const suggestUrl = form.dataset.suggestUrl;
    const emailDomain = form.dataset.emailDomain;

    // Automatické doplňování běží, jen když je pole Username prázdné:
    //   - Nový student: na začátku ano.
    //   - Po neúspěšném uložení nebo při úpravě už v poli něco je → nepřepisovat bez vyžádání
    //     (nový návrh jen tlačítkem Navrhnout znovu).
    let autoFill = usernameInput.value === '';
    let timer = null;
    let lastRequestId = 0;

    function updateUpn() {
        const username = usernameInput.value.trim();
        upnPreview.textContent = (username || '…') + '@' + emailDomain;
    }

    function showReview(reason) {
        // PROČ textContent (ne innerHTML): text se nikdy nevykoná jako HTML.
        reviewHint.textContent = reason ? 'Ke kontrole: ' + reason : '';
        reviewHint.classList.toggle('d-none', !reason);
    }

    async function suggest() {
        const firstName = firstNameInput.value.trim();
        const lastName = lastNameInput.value.trim();
        if (!firstName || !lastName) {
            return;
        }

        // PROČ číslo požadavku: odpověď na starší dotaz může přijít až po novější.
        //   Použijeme jen odpověď na poslední dotaz.
        const requestId = ++lastRequestId;

        const url = new URL(suggestUrl, window.location.origin);
        url.searchParams.set('firstName', firstName);
        url.searchParams.set('lastName', lastName);

        try {
            const response = await fetch(url);
            if (!response.ok || requestId !== lastRequestId || !autoFill) {
                return;
            }
            const data = await response.json();
            usernameInput.value = data.username;
            showReview(data.needsReview ? data.reviewReason : null);
            updateUpn();
        } catch {
            // Server nedostupný – admin username vyplní ručně.
        }
    }

    // Návrh až 400 ms po posledním stisku klávesy, ne po každém písmenu.
    function scheduleSuggest() {
        if (!autoFill) {
            return;
        }
        clearTimeout(timer);
        timer = setTimeout(suggest, 400);
    }

    firstNameInput.addEventListener('input', scheduleSuggest);
    lastNameInput.addEventListener('input', scheduleSuggest);

    // Admin píše do pole Username → přestat ho přepisovat.
    usernameInput.addEventListener('input', () => {
        autoFill = false;
        showReview(null);
        updateUpn();
    });

    suggestButton.addEventListener('click', () => {
        autoFill = true;
        suggest();
    });

    updateUpn();
}
