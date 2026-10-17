/* Coastal Editorial — interaction layer for the sea-and-sun portfolio. Data is intentionally local for now; replace the array with fetch('data/assignments.json') when the JSON workflow is ready. */

const assignments = [
  {
    number: "01",
    title: "Bài tập 1",
    description: "Nội dung tạm — chưa cập nhật.",
    category: "Tạm nhập",
    link: "#"
  },
  {
    number: "02",
    title: "Bài tập 2",
    description: "Nội dung tạm — chưa cập nhật.",
    category: "Tạm nhập",
    link: "#"
  },
  {
    number: "03",
    title: "Bài tập 3",
    description: "Nội dung tạm — chưa cập nhật.",
    category: "Tạm nhập",
    link: "#"
  }
];

function assignmentCard(item) {
  const external = item.link !== "#";
  return `
    <div class="col-md-6 col-xl-4 reveal">
      <article class="assignment-card d-flex flex-column">
        <div class="d-flex justify-content-between align-items-center">
          <span class="assignment-number">BÀI ${item.number}</span>
          <span class="badge badge-sea rounded-0 px-2 py-1">${item.category}</span>
        </div>
        <h3>${item.title}</h3>
        <p class="mb-4">${item.description}</p>
        <a class="assignment-link mt-auto" href="${item.link}" ${external ? 'target="_blank" rel="noreferrer"' : ''}>
          ${external ? 'Xem bài tập ↗' : 'Sẽ cập nhật →'}
        </a>
      </article>
    </div>`;
}

function renderAssignments(items = assignments) {
  const container = document.querySelector("#assignment-grid");
  if (!container) return;
  container.innerHTML = items.map(assignmentCard).join("");
}

async function loadAssignments() {
  try {
    const response = await fetch("data/assignments.json", { cache: "no-store" });
    if (!response.ok) throw new Error("Không thể tải dữ liệu JSON");
    renderAssignments(await response.json());
  } catch (error) {
    renderAssignments(assignments);
  }
}

document.addEventListener("DOMContentLoaded", () => {
  loadAssignments();
  document.querySelector("#current-year").textContent = new Date().getFullYear();
});
